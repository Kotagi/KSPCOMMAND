using System;
using System.Collections.Generic;

namespace KspWebMap
{
    public sealed class BodyTextureExportRegistry
    {
        private readonly Dictionary<string, BodyTextureExportState> _states =
            new Dictionary<string, BodyTextureExportState>(StringComparer.Ordinal);

        public bool TryGetState(string bodyName, out BodyTextureExportState state)
        {
            if (string.IsNullOrEmpty(bodyName))
            {
                state = null;
                return false;
            }

            return _states.TryGetValue(bodyName, out state);
        }

        public void SetState(BodyTextureExportState state)
        {
            if (state == null || string.IsNullOrEmpty(state.BodyName))
            {
                return;
            }

            _states[state.BodyName] = state;
        }

        public void EnsurePending(string bodyName)
        {
            BodyTextureExportState existing;

            if (_states.TryGetValue(bodyName, out existing)
                && existing != null
                && existing.Status != BodyTextureExportState.StatusFailed)
            {
                return;
            }

            SetState(new BodyTextureExportState
            {
                BodyName = bodyName,
                Status = BodyTextureExportState.StatusPending
            });
        }

        public IEnumerable<BodyTextureExportState> GetAllStates()
        {
            return _states.Values;
        }
    }
}
