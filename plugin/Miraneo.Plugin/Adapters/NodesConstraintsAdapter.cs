using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Miraneo.Core;
using UnityEngine;
using SNeo = global::Studio;

namespace Miraneo.Adapters
{
    // NodesConstraints (joan6694, com.joan6694.illusionplugins.nodesconstraints
    // 1.3.3) ships no reference assembly and, unlike Timeline, exposes nothing at
    // all: the plugin field, the constraint list, both AddConstraint overloads,
    // RemoveConstraintAt and SetConstraintEnabled are private, and Constraint
    // itself is a private nested type. Everything is resolved by reflection in a
    // single all-or-nothing Bind(), so a version that moved a member reports the
    // bridge unavailable instead of half-working. Main thread only.
    internal static class NodesConstraintsAdapter
    {
        public const string Guid = "com.joan6694.illusionplugins.nodesconstraints";

        private const BindingFlags Inst = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        private const BindingFlags Stat = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

        private static bool _tried;
        private static string _error;
        private static string _version;

        private static Type _tPlugin, _tConstraint, _tLock;
        private static FieldInfo _fSelf, _fConstraints;
        private static MethodInfo _mAdd, _mRemoveAt, _mSetEnabled;
        private static ConstructorInfo _ctorLock;
        private static FieldInfo _fLockX, _fLockY, _fLockZ;
        private static FieldInfo _fEnabled, _fParentTransform, _fChildTransform, _fAlias, _fFixDynamicBone, _fDestroyed;
        private static FieldInfo _fPosition, _fRotation, _fScale, _fLookAt;
        private static FieldInfo _fMirrorPosition, _fMirrorRotation, _fMirrorScale;
        private static FieldInfo _fPositionOffset, _fRotationOffset, _fScaleOffset;
        private static FieldInfo _fPositionFactor, _fRotationFactor, _fScaleFactor;
        private static FieldInfo _fPositionDamp, _fRotationDamp, _fScaleDamp;
        private static FieldInfo _fPositionLocks, _fRotationLocks, _fScaleLocks;
        private static FieldInfo _fResetPosition, _fResetRotation, _fResetScale;

        public static bool Available
        {
            get
            {
                Bind();
                return _error == null && _fSelf.GetValue(null) != null;
            }
        }

        // Reported by studio_get_info so an agent can tell "not installed" from
        // "installed but not initialised" before calling the write tools.
        public static string Version
        {
            get
            {
                Bind();
                return _error == null ? _version : null;
            }
        }

        private static object Require()
        {
            Bind();
            if (_error != null)
                throw new StudioApiException(StudioApiException.OperationFailed,
                    "the NodesConstraints plugin (" + Guid + ") is not available: " + _error);
            var self = _fSelf.GetValue(null);
            if (self == null)
                throw new StudioApiException(StudioApiException.StudioNotReady,
                    "NodesConstraints " + _version + " is loaded but has not initialised yet; open a Studio scene and try again");
            return self;
        }

        private static void Bind()
        {
            if (_tried) return;
            _tried = true;
            try
            {
                _tPlugin = FindType("NodesConstraints.NodesConstraints");
                if (_tPlugin == null)
                {
                    _error = "assembly not loaded (plugin not installed)";
                    return;
                }
                _tConstraint = Nested(_tPlugin, "Constraint");
                _tLock = Nested(_tPlugin, "TransformLock");
                _version = Convert.ToString(Field(_tPlugin, "Version", Stat).GetRawConstantValue());

                _fSelf = Field(_tPlugin, "_self", Stat);
                _fConstraints = Field(_tPlugin, "_constraints", Inst);
                _mRemoveAt = Method(_tPlugin, "RemoveConstraintAt", Inst, new[] { typeof(int) });
                _mSetEnabled = Method(_tPlugin, "SetConstraintEnabled", Inst, new[] { _tConstraint, typeof(bool) });
                // The 26-argument overload; the 10-argument one forwards to it with
                // mirroring, look-at, factors, damping and locks fixed at defaults.
                _mAdd = Method(_tPlugin, "AddConstraint", Inst, new[]
                {
                    typeof(bool), typeof(Transform), typeof(Transform),
                    typeof(bool), typeof(bool), typeof(Vector3),
                    typeof(bool), typeof(bool), typeof(bool), typeof(Quaternion),
                    typeof(bool), typeof(bool), typeof(Vector3),
                    typeof(string),
                    typeof(float), typeof(float), typeof(float),
                    typeof(float), typeof(float), typeof(float),
                    _tLock, _tLock, _tLock,
                    typeof(bool), typeof(bool), typeof(bool),
                });

                _ctorLock = Ctor(_tLock, new[] { typeof(bool), typeof(bool), typeof(bool) });
                _fLockX = Field(_tLock, "x", Inst);
                _fLockY = Field(_tLock, "y", Inst);
                _fLockZ = Field(_tLock, "z", Inst);

                _fEnabled = Field(_tConstraint, "enabled", Inst);
                _fParentTransform = Field(_tConstraint, "parentTransform", Inst);
                _fChildTransform = Field(_tConstraint, "childTransform", Inst);
                _fAlias = Field(_tConstraint, "alias", Inst);
                _fFixDynamicBone = Field(_tConstraint, "fixDynamicBone", Inst);
                _fDestroyed = Field(_tConstraint, "destroyed", Inst);
                _fPosition = Field(_tConstraint, "position", Inst);
                _fRotation = Field(_tConstraint, "rotation", Inst);
                _fScale = Field(_tConstraint, "scale", Inst);
                _fLookAt = Field(_tConstraint, "lookAt", Inst);
                _fMirrorPosition = Field(_tConstraint, "mirrorPosition", Inst);
                _fMirrorRotation = Field(_tConstraint, "mirrorRotation", Inst);
                _fMirrorScale = Field(_tConstraint, "mirrorScale", Inst);
                _fPositionOffset = Field(_tConstraint, "positionOffset", Inst);
                _fRotationOffset = Field(_tConstraint, "rotationOffset", Inst);
                _fScaleOffset = Field(_tConstraint, "scaleOffset", Inst);
                _fPositionFactor = Field(_tConstraint, "positionChangeFactor", Inst);
                _fRotationFactor = Field(_tConstraint, "rotationChangeFactor", Inst);
                _fScaleFactor = Field(_tConstraint, "scaleChangeFactor", Inst);
                _fPositionDamp = Field(_tConstraint, "positionDamp", Inst);
                _fRotationDamp = Field(_tConstraint, "rotationDamp", Inst);
                _fScaleDamp = Field(_tConstraint, "scaleDamp", Inst);
                _fPositionLocks = Field(_tConstraint, "positionLocks", Inst);
                _fRotationLocks = Field(_tConstraint, "rotationLocks", Inst);
                _fScaleLocks = Field(_tConstraint, "scaleLocks", Inst);
                _fResetPosition = Field(_tConstraint, "resetOriginalPosition", Inst);
                _fResetRotation = Field(_tConstraint, "resetOriginalRotation", Inst);
                _fResetScale = Field(_tConstraint, "resetOriginalScale", Inst);
            }
            catch (Exception e)
            {
                _error = e.Message;
            }
        }

        private static Type FindType(string name)
        {
            var t = Type.GetType(name + ",NodesConstraints", false);
            if (t != null) return t;
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try { t = asm.GetType(name, false); }
                catch { continue; }
                if (t != null) return t;
            }
            return null;
        }

        private static Type Nested(Type t, string name)
        {
            var n = t.GetNestedType(name, BindingFlags.Public | BindingFlags.NonPublic);
            if (n == null) throw new MissingMemberException("type " + t.Name + "." + name + " is gone (unsupported NodesConstraints version)");
            return n;
        }

        private static FieldInfo Field(Type t, string name, BindingFlags flags)
        {
            var f = t.GetField(name, flags);
            if (f == null) throw new MissingMemberException("field " + t.Name + "." + name + " is gone (unsupported NodesConstraints version)");
            return f;
        }

        private static MethodInfo Method(Type t, string name, BindingFlags flags, Type[] args)
        {
            var m = t.GetMethod(name, flags, null, args, null);
            if (m == null) throw new MissingMemberException("method " + t.Name + "." + name + " is gone (unsupported NodesConstraints version)");
            return m;
        }

        private static ConstructorInfo Ctor(Type t, Type[] args)
        {
            var c = t.GetConstructor(Inst, null, args, null);
            if (c == null) throw new MissingMemberException("constructor " + t.Name + " is gone (unsupported NodesConstraints version)");
            return c;
        }

        // ---- reading -----------------------------------------------------

        public static ConstraintListDto List(int? objectId)
        {
            Bind();
            var dto = new ConstraintListDto { Available = false, Unavailable = _error, Version = _version };
            if (_error != null) return dto;
            var self = _fSelf.GetValue(null);
            if (self == null)
            {
                dto.Unavailable = "NodesConstraints is loaded but has not initialised yet";
                return dto;
            }

            dto.Available = true;
            dto.Constraints = new List<ConstraintDto>();

            SNeo.ObjectCtrlInfo filter = null;
            if (objectId.HasValue) filter = StudioAdapter.RequireObject(objectId.Value);

            foreach (var constraint in (IList)_fConstraints.GetValue(self))
            {
                dto.Count++;
                var entry = ToDto(constraint);
                if (filter != null && entry.Parent.ObjectId != objectId && entry.Child.ObjectId != objectId) continue;
                dto.Constraints.Add(entry);
            }
            return dto;
        }

        private static ConstraintDto ToDto(object constraint)
        {
            var parent = _fParentTransform.GetValue(constraint) as Transform;
            var child = _fChildTransform.GetValue(constraint) as Transform;
            return new ConstraintDto
            {
                Id = constraint.GetHashCode(),
                Enabled = (bool)_fEnabled.GetValue(constraint),
                Alias = Convert.ToString(_fAlias.GetValue(constraint)),
                Parent = Describe(parent),
                Child = Describe(child),
                Position = (bool)_fPosition.GetValue(constraint),
                Rotation = (bool)_fRotation.GetValue(constraint),
                Scale = (bool)_fScale.GetValue(constraint),
                LookAt = (bool)_fLookAt.GetValue(constraint),
                MirrorPosition = (bool)_fMirrorPosition.GetValue(constraint),
                MirrorRotation = (bool)_fMirrorRotation.GetValue(constraint),
                MirrorScale = (bool)_fMirrorScale.GetValue(constraint),
                PositionOffset = Vec((Vector3)_fPositionOffset.GetValue(constraint)),
                RotationOffset = Vec(((Quaternion)_fRotationOffset.GetValue(constraint)).eulerAngles),
                ScaleOffset = Vec((Vector3)_fScaleOffset.GetValue(constraint)),
                PositionFactor = (float)_fPositionFactor.GetValue(constraint),
                RotationFactor = (float)_fRotationFactor.GetValue(constraint),
                ScaleFactor = (float)_fScaleFactor.GetValue(constraint),
                PositionDamp = (float)_fPositionDamp.GetValue(constraint),
                RotationDamp = (float)_fRotationDamp.GetValue(constraint),
                ScaleDamp = (float)_fScaleDamp.GetValue(constraint),
                PositionLocks = ReadLock(_fPositionLocks.GetValue(constraint)),
                RotationLocks = ReadLock(_fRotationLocks.GetValue(constraint)),
                ScaleLocks = ReadLock(_fScaleLocks.GetValue(constraint)),
                ResetPosition = (bool)_fResetPosition.GetValue(constraint),
                ResetRotation = (bool)_fResetRotation.GetValue(constraint),
                ResetScale = (bool)_fResetScale.GetValue(constraint),
                FixDynamicBone = (bool)_fFixDynamicBone.GetValue(constraint),
            };
        }

        private static Vec3 Vec(Vector3 v) => new Vec3(v.x, v.y, v.z);

        private static bool[] ReadLock(object value)
        {
            if (value == null) return new[] { true, true, true };
            return new[]
            {
                (bool)_fLockX.GetValue(value),
                (bool)_fLockY.GetValue(value),
                (bool)_fLockZ.GetValue(value),
            };
        }

        // The same walk NodesConstraints' own scene writer does: climb until a
        // transform is some object's guide target, then describe the rest as a
        // path relative to it.
        private static ConstraintNodeDto Describe(Transform t)
        {
            var dto = new ConstraintNodeDto();
            if (t == null) return dto;
            dto.Target = t.name;

            var path = "";
            var current = t;
            while (current != null)
            {
                var id = ObjectIdOf(current);
                if (id.HasValue)
                {
                    dto.ObjectId = id;
                    dto.Path = path.Length == 0 ? "root" : path;
                    if (path.Length == 0) dto.Target = "root";
                    return dto;
                }
                path = path.Length == 0 ? current.name : current.name + "/" + path;
                current = current.parent;
            }
            return dto;
        }

        private static int? ObjectIdOf(Transform t)
        {
            // NodesConstraints sets _self in Awake, well before a Studio scene
            // exists, so the list is readable earlier than the object table.
            if (!StudioAdapter.IsStudioReady()) return null;
            foreach (var kv in Singleton<SNeo.Studio>.Instance.dicObjectCtrl)
            {
                var guide = kv.Value.guideObject;
                if (guide != null && ReferenceEquals(guide.transformTarget, t)) return kv.Key;
            }
            return null;
        }

        // ---- writing -----------------------------------------------------

        public static ConstraintDto Create(ConstraintPatch patch)
        {
            var self = Require();
            if (patch.Parent == null || patch.Child == null)
                throw new StudioApiException(StudioApiException.InvalidParams,
                    "'parent' and 'child' are both required: a constraint makes one node follow another");

            var parent = Resolve(patch.Parent, "parent");
            var child = Resolve(patch.Child, "child");
            if (ReferenceEquals(parent, child))
                throw new StudioApiException(StudioApiException.InvalidParams, "'parent' and 'child' resolve to the same node");

            var position = patch.Position ?? true;
            var rotation = patch.Rotation ?? true;
            var scale = patch.Scale ?? true;
            if (!position && !rotation && !scale)
                throw new StudioApiException(StudioApiException.InvalidParams,
                    "a constraint must link at least one of 'position', 'rotation' or 'scale'");
            // UpdateScale divides by childTransform.parent.lossyScale with no guard,
            // and it runs every frame outside this call: a scene-root child would
            // throw there, not here.
            if (scale && child.parent == null)
                throw new StudioApiException(StudioApiException.InvalidParams,
                    "the child node is at the scene root, where NodesConstraints cannot drive scale; pass scale=false");

            foreach (var existing in (IList)_fConstraints.GetValue(self))
            {
                var p = _fParentTransform.GetValue(existing) as Transform;
                var c = _fChildTransform.GetValue(existing) as Transform;
                if ((ReferenceEquals(p, parent) && ReferenceEquals(c, child)) ||
                    (ReferenceEquals(p, child) && ReferenceEquals(c, parent)))
                    throw new StudioApiException(StudioApiException.InvalidParams,
                        "these two nodes are already constrained (id " + existing.GetHashCode() +
                        "); NodesConstraints allows one constraint per pair, in either direction — change it with studio_constraint_set or drop it first");
            }

            var lookAt = patch.LookAt ?? false;
            var mirrorRotation = patch.MirrorRotation ?? false;

            var args = new object[]
            {
                patch.Enabled ?? true, parent, child,
                position, patch.MirrorPosition ?? false, PositionOffset(patch, parent, child),
                rotation, mirrorRotation, lookAt, RotationOffset(patch, parent, child, lookAt, mirrorRotation),
                scale, patch.MirrorScale ?? false, ScaleOffset(patch, parent, child),
                patch.Alias ?? "",
                patch.PositionFactor ?? 1f, patch.RotationFactor ?? 1f, patch.ScaleFactor ?? 1f,
                patch.PositionDamp ?? 0f, patch.RotationDamp ?? 0f, patch.ScaleDamp ?? 0f,
                MakeLock(patch.PositionLocks), MakeLock(patch.RotationLocks), MakeLock(patch.ScaleLocks),
                patch.ResetPosition ?? true, patch.ResetRotation ?? true, patch.ResetScale ?? true,
            };

            var constraint = _mAdd.Invoke(self, args);
            if (constraint == null)
                throw new StudioApiException(StudioApiException.OperationFailed, "NodesConstraints refused the constraint");
            if (patch.FixDynamicBone.HasValue) _fFixDynamicBone.SetValue(constraint, patch.FixDynamicBone.Value);
            return ToDto(constraint);
        }

        public static ConstraintDto Set(int id, ConstraintPatch patch)
        {
            var self = Require();
            if (patch.Parent != null || patch.Child != null)
                throw new StudioApiException(StudioApiException.InvalidParams,
                    "a constraint's two nodes are fixed; delete it and create another to re-target");

            var constraint = Find(self, id);
            var parent = _fParentTransform.GetValue(constraint) as Transform;
            var child = _fChildTransform.GetValue(constraint) as Transform;
            if (parent == null || child == null)
                throw new StudioApiException(StudioApiException.OperationFailed,
                    "constraint " + id + " has lost one of its nodes and NodesConstraints will drop it on its next frame");

            var scale = patch.Scale ?? (bool)_fScale.GetValue(constraint);
            if (scale && child.parent == null)
                throw new StudioApiException(StudioApiException.InvalidParams,
                    "the child node is at the scene root, where NodesConstraints cannot drive scale; pass scale=false");
            var position = patch.Position ?? (bool)_fPosition.GetValue(constraint);
            var rotation = patch.Rotation ?? (bool)_fRotation.GetValue(constraint);
            if (!position && !rotation && !scale)
                throw new StudioApiException(StudioApiException.InvalidParams,
                    "a constraint must link at least one of 'position', 'rotation' or 'scale'");

            if (patch.Alias != null) _fAlias.SetValue(constraint, patch.Alias);
            if (patch.Position.HasValue) _fPosition.SetValue(constraint, patch.Position.Value);
            if (patch.Rotation.HasValue) _fRotation.SetValue(constraint, patch.Rotation.Value);
            if (patch.Scale.HasValue) _fScale.SetValue(constraint, patch.Scale.Value);
            if (patch.LookAt.HasValue) _fLookAt.SetValue(constraint, patch.LookAt.Value);
            if (patch.MirrorPosition.HasValue) _fMirrorPosition.SetValue(constraint, patch.MirrorPosition.Value);
            if (patch.MirrorRotation.HasValue) _fMirrorRotation.SetValue(constraint, patch.MirrorRotation.Value);
            if (patch.MirrorScale.HasValue) _fMirrorScale.SetValue(constraint, patch.MirrorScale.Value);
            if (patch.PositionOffset.HasValue) _fPositionOffset.SetValue(constraint, V(patch.PositionOffset.Value));
            if (patch.RotationOffset.HasValue) _fRotationOffset.SetValue(constraint, Q(patch.RotationOffset.Value));
            if (patch.ScaleOffset.HasValue) _fScaleOffset.SetValue(constraint, V(patch.ScaleOffset.Value));
            if (patch.PositionFactor.HasValue) _fPositionFactor.SetValue(constraint, patch.PositionFactor.Value);
            if (patch.RotationFactor.HasValue) _fRotationFactor.SetValue(constraint, patch.RotationFactor.Value);
            if (patch.ScaleFactor.HasValue) _fScaleFactor.SetValue(constraint, patch.ScaleFactor.Value);
            if (patch.PositionDamp.HasValue) _fPositionDamp.SetValue(constraint, patch.PositionDamp.Value);
            if (patch.RotationDamp.HasValue) _fRotationDamp.SetValue(constraint, patch.RotationDamp.Value);
            if (patch.ScaleDamp.HasValue) _fScaleDamp.SetValue(constraint, patch.ScaleDamp.Value);
            if (patch.PositionLocks != null) _fPositionLocks.SetValue(constraint, MakeLock(patch.PositionLocks));
            if (patch.RotationLocks != null) _fRotationLocks.SetValue(constraint, MakeLock(patch.RotationLocks));
            if (patch.ScaleLocks != null) _fScaleLocks.SetValue(constraint, MakeLock(patch.ScaleLocks));
            if (patch.ResetPosition.HasValue) _fResetPosition.SetValue(constraint, patch.ResetPosition.Value);
            if (patch.ResetRotation.HasValue) _fResetRotation.SetValue(constraint, patch.ResetRotation.Value);
            if (patch.ResetScale.HasValue) _fResetScale.SetValue(constraint, patch.ResetScale.Value);
            if (patch.FixDynamicBone.HasValue) _fFixDynamicBone.SetValue(constraint, patch.FixDynamicBone.Value);

            // Last, and never as a plain field write: SetConstraintEnabled restores
            // the child's captured transform on the way down and re-captures it on
            // the way up, both according to the flags this call has just set.
            if (patch.Enabled.HasValue && patch.Enabled.Value != (bool)_fEnabled.GetValue(constraint))
                _mSetEnabled.Invoke(self, new[] { constraint, (object)patch.Enabled.Value });

            return ToDto(constraint);
        }

        public static ConstraintListDto Delete(List<int> ids)
        {
            var self = Require();
            var constraints = (IList)_fConstraints.GetValue(self);
            var indices = new List<int>(ids.Count);
            foreach (var id in ids)
            {
                var index = IndexOf(constraints, id);
                if (index < 0)
                    throw new StudioApiException(StudioApiException.NotFound,
                        "no constraint " + id + " (list them with studio_constraint_list)");
                if (!indices.Contains(index)) indices.Add(index);
            }
            // Descending, so an earlier removal never shifts an index still to come.
            indices.Sort();
            for (var i = indices.Count - 1; i >= 0; i--)
                _mRemoveAt.Invoke(self, new object[] { indices[i] });
            return List(null);
        }

        private static object Find(object self, int id)
        {
            var constraints = (IList)_fConstraints.GetValue(self);
            var index = IndexOf(constraints, id);
            if (index < 0)
                throw new StudioApiException(StudioApiException.NotFound,
                    "no constraint " + id + " (list them with studio_constraint_list)");
            return constraints[index];
        }

        private static int IndexOf(IList constraints, int id)
        {
            for (var i = 0; i < constraints.Count; i++)
                if (constraints[i].GetHashCode() == id && !(bool)_fDestroyed.GetValue(constraints[i])) return i;
            return -1;
        }

        // ---- offsets -----------------------------------------------------

        // Omitting an offset captures the nodes' current relation, which is what
        // NodesConstraints' own "Use current" buttons compute — so creating a
        // constraint freezes the pose instead of snapping the child onto the
        // parent, which is what its zero/identity defaults would do.
        private static Vector3 PositionOffset(ConstraintPatch patch, Transform parent, Transform child) =>
            patch.PositionOffset.HasValue ? V(patch.PositionOffset.Value) : parent.InverseTransformPoint(child.position);

        private static Quaternion RotationOffset(ConstraintPatch patch, Transform parent, Transform child, bool lookAt, bool mirror)
        {
            if (patch.RotationOffset.HasValue) return Q(patch.RotationOffset.Value);
            if (!lookAt) return Quaternion.Inverse(parent.rotation) * child.rotation;
            var look = Quaternion.LookRotation(parent.position - child.position);
            if (mirror) look = new Quaternion(look.x * -1f, look.y * -1f, look.z, look.w);
            return Quaternion.Inverse(look) * child.rotation;
        }

        private static Vector3 ScaleOffset(ConstraintPatch patch, Transform parent, Transform child)
        {
            if (patch.ScaleOffset.HasValue) return V(patch.ScaleOffset.Value);
            var p = parent.lossyScale;
            var c = child.lossyScale;
            return new Vector3(
                p.x == 0f ? 1f : c.x / p.x,
                p.y == 0f ? 1f : c.y / p.y,
                p.z == 0f ? 1f : c.z / p.z);
        }

        private static Vector3 V(Vec3 v) => new Vector3(v.X, v.Y, v.Z);

        private static Quaternion Q(Vec3 euler) => Quaternion.Euler(euler.X, euler.Y, euler.Z);

        private static object MakeLock(bool[] axes) =>
            axes == null ? null : _ctorLock.Invoke(new object[] { axes[0], axes[1], axes[2] });

        // ---- node resolution ---------------------------------------------

        // A constraint links two arbitrary transforms, so 'target' accepts more
        // than the guide objects the other tools expose: a bone or IK target name
        // (studio_list_bones), an attach-point bone (studio_list_attach_points), a
        // slash path under the object's root, or any descendant's name.
        private static Transform Resolve(ConstraintEndpoint endpoint, string field)
        {
            var oci = StudioAdapter.RequireObject(endpoint.ObjectId);
            if (oci.guideObject == null || oci.guideObject.transformTarget == null)
                throw new StudioApiException(StudioApiException.OperationFailed,
                    "object " + endpoint.ObjectId + " has no transform NodesConstraints can bind to");
            var root = oci.guideObject.transformTarget;
            var target = endpoint.Target;
            if (string.IsNullOrEmpty(target) || target == "root") return root;

            var chr = oci as SNeo.OCIChar;
            if (chr != null)
            {
                SNeo.OCIChar.BoneInfo bone;
                if (StudioAdapter.BoneIndex(chr).TryGetValue(target, out bone) &&
                    bone.guideObject != null && bone.guideObject.transformTarget != null)
                    return bone.guideObject.transformTarget;

                var index = StudioAdapter.IkTargetIndex(target);
                if (index >= 0 && index < chr.listIKTarget.Count)
                {
                    var guide = chr.listIKTarget[index].guideObject;
                    if (guide != null && guide.transformTarget != null) return guide.transformTarget;
                }
            }

            var found = root.Find(target) ?? Descendant(root, target);
            if (found == null)
                throw new StudioApiException(StudioApiException.InvalidParams,
                    "'" + field + ".target': object " + endpoint.ObjectId + " has no node '" + target +
                    "' (names come from studio_list_bones, studio_list_attach_points, the 'path' of a listed constraint, or 'root' for the object itself)");
            return found;
        }

        private static Transform Descendant(Transform root, string name)
        {
            for (var i = 0; i < root.childCount; i++)
            {
                var child = root.GetChild(i);
                if (child.name == name) return child;
            }
            for (var i = 0; i < root.childCount; i++)
            {
                var found = Descendant(root.GetChild(i), name);
                if (found != null) return found;
            }
            return null;
        }
    }
}
