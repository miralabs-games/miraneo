using System.Collections.Generic;
using Miraneo.Adapters;
using Miraneo.Core;

namespace Miraneo.Services
{
    // Constraint edits all land within one frame, so like the timeline service
    // none of this needs a coroutine, and it stays off IStudioApi because
    // NodesConstraints is optional: that contract describes the vanilla surface.
    public static class ConstraintService
    {
        public static string Version => NodesConstraintsAdapter.Version;

        public static ConstraintListDto List(int? objectId) => NodesConstraintsAdapter.List(objectId);
        public static ConstraintDto Create(ConstraintPatch patch) => NodesConstraintsAdapter.Create(patch);
        public static ConstraintDto Set(int id, ConstraintPatch patch) => NodesConstraintsAdapter.Set(id, patch);
        public static ConstraintListDto Delete(List<int> ids) => NodesConstraintsAdapter.Delete(ids);
    }
}
