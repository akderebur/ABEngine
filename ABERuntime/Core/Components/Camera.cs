using System;
using System.Linq;
using System.Numerics;
using Halak;
using Newtonsoft.Json;
using ABEngine.ABERuntime.ECS;

namespace ABEngine.ABERuntime.Components
{
    public class Camera : ABComponent
    {
        //private Transform _followTarget;
        //private string _followEntGuid;

        //public Transform followTarget
        //{
        //    get
        //    {
        //        return _followTarget;
        //    }

        //    set
        //    {
        //        _followTarget = value;
        //        if(value != null)
        //            _followEntGuid = value.entity.Get<Guid>().ToString();
        //    }
        //}

        // Cam Fields
        private bool _isActive;
        private bool _lastActive;

        // Cam Props
        public Transform followTarget { get; set; }
        public float speed { get; set; }
        public bool ignoreY { get; set; }
        public Vector3 offset { get; set; }
        public bool followInFixedUpdate { get; set; }
        public float cutoffY { get; set; }

        public Vector3 velocity;
        public bool isActive
        {
            get { return _isActive; }
            set
            {
                _isActive = value;
                if(value != _lastActive)
                    Game.TriggerCamCheck();
                _lastActive = _isActive;
            }
        }


        public Camera()
        {
            speed = 1f;
            ignoreY = true;
            offset = Vector3.Zero;
            _isActive = true;
            _lastActive = _isActive;
        }

        //public void SetReferences()
        //{
        //    if(!string.IsNullOrEmpty(_followEntGuid))
        //        followTarget = Game.GameWorld.GetEntities().FirstOrDefault(e => e.Get<Guid>().Equals(Guid.Parse(_followEntGuid))).Get<Transform>();
        //}

        //public JValue Serialize()
        //{
        //    JsonObjectBuilder jObj = new JsonObjectBuilder(500);
        //    jObj.Put("type", GetType().ToString());
        //    jObj.Put("FollowTarget", string.IsNullOrEmpty(_followEntGuid) ? "" : _followEntGuid);
        //    jObj.Put("Speed", speed);
        //    jObj.Put("OffsetX", offset.X);
        //    jObj.Put("OffsetY", offset.Y);
        //    jObj.Put("OffsetZ", offset.Z);
        //    jObj.Put("FollowFixed", followInFixedUpdate);
        //    jObj.Put("IsActive", isActive);


        //    return jObj.Build();
        //}

        //public void Deserialize(string json)
        //{
        //    JValue data = JValue.Parse(json);
        //    _followEntGuid = data["FollowTarget"];
        //    speed = data["Speed"];
        //    offset = new Vector3(data["OffsetX"], data["OffsetY"], data["OffsetZ"]);
        //    followInFixedUpdate = data["FollowFixed"];
        //    isActive = data["IsActive"];
        //}
    }
}
