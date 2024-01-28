﻿using System;
using System.Linq;
using System.Numerics;
using Halak;
using Newtonsoft.Json;

namespace ABEngine.ABERuntime.Components
{
    public class Camera : ABComponent
    {
        // Cam Fields
        private bool _isActive;
        private bool _lastActive;
        private CameraProjection _projection;
        public Vector3 velocity;
        private Vector2 _viewSize;

        // Cam Props
        public Transform followTarget { get; set; }
        public float speed { get; set; }
        public bool ignoreY { get; set; }
        public Vector3 offset { get; set; }
        public bool followInFixedUpdate { get; set; }
        public float cutoffY { get; set; }
        public CameraProjection cameraProjection
        {
            get { return _projection; }
            set
            {
                _projection = value;
                Game.RefreshProjection(Game.canvas);
            }
        }

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

        public bool matchCanvas { get; set; }
        public Vector2 viewSize
        {
            get { return _viewSize; }
            set
            {
                _viewSize = value;
                Game.RefreshProjection(Game.canvas);
            }
        }
        internal Vector2 compViewSize { get; set; }

        public Vector4 viewport { get; set; }

        public Camera()
        {
            speed = 1f;
            ignoreY = true;
            offset = Vector3.Zero;
            _isActive = true;
            _lastActive = _isActive;
            matchCanvas = true;
            viewSize = Vector2.One;
            viewport = new Vector4(0f, 0f, 1f, 1f);
        }

        internal void OnCameraActivate()
        {
            compViewSize = viewSize;
            if (matchCanvas)
            {
                compViewSize *= Game.canvas.canvasSize;
            }
        }

        public Vector3 ScreenToWorld(Vector2 screenPos, float depth = 0f)
        {
            if (_projection == CameraProjection.Orthographic)
                return ScreenToViewport(screenPos).NormalizedToWorld();
            else
                return ScreenToViewport(screenPos).NormalizedToWorldPerspective(depth);
        }

        //public Vector3 ScreenViewportToWorld(Vector2 screenPos, float depth = 0f)
        //{
        //    //screenPos.X -= Game.virtualSize.X * viewport.X;
        //    //screenPos.Y -= Game.virtualSize.Y * (1f - viewport.W - viewport.Y);

        //    if (_projection == CameraProjection.Orthographic)
        //        return ScreenToViewport(screenPos).NormalizedToWorld();
        //    else
        //        return screenPos.ScreenToWorldPerspective(depth);
        //}

        public Vector2 ScreenToViewport(Vector2 screenPos)
        {
            Vector2 normalized = (screenPos / Game.virtualSize);
            
            normalized.X -= viewport.X;
            normalized.X /= viewport.Z;

            float y = 1f - normalized.Y;
            y -= viewport.Y;
            y /= viewport.W;
            normalized.Y = 1f - y;

            return normalized;
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

    public enum CameraProjection
    {
        Orthographic,
        Perspective
    }
}
