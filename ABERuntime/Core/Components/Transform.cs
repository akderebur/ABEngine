using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Halak;
using ABEngine.ABERuntime.Core.MathA;
using Arch.Core;
using Arch.Core.Extensions;

namespace ABEngine.ABERuntime.Components
{
    public class Transform : JSerializable
    {
        private Vector3 _localPosition;
        private Quaternion _localRotation;
        private Vector3 _localScale;
        private Vector3 _localEulerAngles;

        private Vector3 _worldPosition;
        private Quaternion _worldRotation;
        private Vector3 _worldScale;

        public Matrix4x4 localMatrix;
        public Matrix4x4 worldMatrix;
        private Matrix4x4 _worldToLocalMatrix;

        private Transform _parent;

        //internal bool enabled = true;

        private bool _enabled = true;
        public bool enabled
        {
            get { return _enabled; }
            set
            {
                if(_enabled != value)
                {
                    _enabled = value;
                    foreach (var child in children)
                    {
                        child.enabled = _enabled;
                    }
                }
            }
        }

        public List<Transform> children { get; private set; }
        public string name { get { return entity.Get<string>(); } }

        [JSerialize]
        public bool isStatic { get; internal set; }
        public Entity entity { get; private set; }
        internal bool transformMove { get; set; }
        internal string parentGuidStr { get; private set; }

        [JSerialize]
        public string tag { get; set; }

        bool keepWorldPos = true;

        public Transform()
        {
            tag = "None";
            isStatic = false;
            Init();
        }

        public Transform(string tag)
        {
            this.tag = tag;
            this.isStatic = false;
            Init();
        }

        public Transform(string tag, bool isStatic)
        {
            this.tag = tag;
            this.isStatic = isStatic;
            Init();
        }

        void Init()
        {
            _localPosition = Vector3.Zero;
            _localRotation = Quaternion.Identity;
            _localScale = Vector3.One;

            children = new List<Transform>();
            RecalculateTRS();
        }

        public void SetEntity(in Entity ent)
        {
            this.entity = ent;
        }

        //internal void EnableChildren()
        //{
        //    foreach (var child in children)
        //    {
        //        child.entity.SetEnabled(true);
        //    }
        //}


        //internal void DisableChildren()
        //{
        //    foreach (var child in children)
        //    {
        //        child.entity.SetEnabled(false);
        //    }
        //}

        public void SetTRS(Vector3 position, Quaternion rotation, Vector3 scale)
        {
            _localPosition = position;
            _localRotation = rotation;
            _localScale = scale;

            RecalculateTRS();
        }

        private void RecalculateTRS()
        {
            localMatrix = Matrix4x4.CreateScale(_localScale) * Matrix4x4.CreateFromQuaternion(_localRotation) * Matrix4x4.CreateTranslation(_localPosition);
            Matrix4x4 worldMat = localMatrix;
            if (_parent != null)
                worldMat *= _parent.worldMatrix;

            this.worldMatrix = worldMat;
            Matrix4x4.Decompose(worldMat, out _worldScale, out _worldRotation, out _worldPosition);

            transformMove = true;

            // Children
            foreach (var transform in this.children)
            {
                transform.RecalculateTRS();
            }
        }

        public JValue Serialize()
        {
            JsonObjectBuilder jObj = new JsonObjectBuilder(500);
            jObj.Put("type", GetType().ToString());
            jObj.Put("Tag", tag);
            jObj.Put("ParentGuid", _parent == null ? "" : _parent.entity.Get<Guid>().ToString());
            jObj.Put("Static", isStatic);
            jObj.Put("Position", _localPosition);
            jObj.Put("Rotation", _localRotation);
            jObj.Put("Scale", _localScale);
            return jObj.Build();



            /*
            var JDict = new Dictionary<string, JValue>()
            {
                {"Type", this.GetType().ToString() },
                {"PosX", _localPosition.X},
                {"PosY", _localPosition.Y},
                {"PosZ", _localPosition.Z},
                {"ScaX", _localScale.X},
                {"ScaY", _localScale.Y},
                {"ScaZ", _localScale.Z},
            };*/

            //return new JValue(JDict).ToString();

            //return JsonSerializer.Serialize(this);

        }

        public void Deserialize(string json)
        {
            JValue data = JValue.Parse(json);
            tag = data["Tag"];
            isStatic = data["Static"];

            if (SceneManager.sceneVersion == 0) // Old serialize
            {
                _localRotation = Quaternion.Identity;
                _localPosition = new Vector3(data["PosX"], data["PosY"], data["PosZ"]);
                _localScale = new Vector3(data["ScaX"], data["ScaY"], data["ScaZ"]);
            }
            else
            {
                _localPosition = data["Position"];
                _localRotation = data["Rotation"];
                _localScale = data["Scale"];
                _localEulerAngles = _localRotation.ToEulerAngles();
            }

            parentGuidStr = data["ParentGuid"];
         
            RecalculateTRS();

        }

        protected void AddChild(Transform child)
        {
            //if (!children.Contains(child))
                children.Add(child);
        }


        protected void RemoveChild(Transform child)
        {
            //if (children.Contains(child))
                children.Remove(child);
        }

        #region Properties
        [JSerialize]
        public Vector3 localPosition
        {
            get { return _localPosition; }
            set { _localPosition = value; RecalculateTRS(); }
        }

        [JSerialize]
        public Vector3 localScale
        {
            get { return _localScale; }
            set { _localScale = value; RecalculateTRS(); }
        }

        [JSerialize]
        public Quaternion localRotation
        {
            get { return _localRotation; }
            set
            {
                _localRotation = value;
                _localEulerAngles = value.ToEulerAngles();
                RecalculateTRS();
            }
        }

        public Vector3 localEulerAngles
        {
            get { return _localEulerAngles; }
            set
            {
                _localEulerAngles = value;
                _localRotation = Quaternion.CreateFromYawPitchRoll(_localEulerAngles.Y, _localEulerAngles.X, _localEulerAngles.Z);
                RecalculateTRS();
            }
        }

        public Vector3 worldPosition
        {
            get { return _worldPosition; }
        }

        public Quaternion worldRotation
        {
            get { return _worldRotation; }
        }

        public Vector3 worldScale
        {
            get { return _worldScale; }
        }

        public Transform parent
        {
            get { return _parent; }
            set
            {
                if (_parent == value)
                    return;

                if (value == null)
                {
                    if (_parent != null)
                        _parent.RemoveChild(this);

                    if (keepWorldPos)
                    {
                        Matrix4x4.Decompose(worldMatrix, out _localScale, out _localRotation, out _localPosition);
                        _parent = value;
                        RecalculateTRS();
                    }

                    _parent = value;
                }
                else //if (value != _parent)
                {
                    if (_parent != null)
                        _parent.RemoveChild(this);

                    if (keepWorldPos)
                    {
                        //float zOrder = _localPosition.Z;
                        Matrix4x4 invPar = Matrix4x4.Identity;
                        Matrix4x4.Invert(value.worldMatrix, out invPar);

                        Matrix4x4 newLocal = worldMatrix * invPar;
                        Matrix4x4.Decompose(newLocal, out _localScale, out _localRotation, out _localPosition);

                        //_localPosition.Z = zOrder;
                    }

                    _parent = value;
                    RecalculateTRS();
                    _parent.AddChild(this);

                }
            }
        }

        public Matrix4x4 worldToLocalMatrix
        {
            get
            {
                Matrix4x4.Invert(worldMatrix, out _worldToLocalMatrix);
                return _worldToLocalMatrix;
            }
        }

        public void SetParent(Transform parent, bool keepWorld)
        {
            keepWorldPos = keepWorld;
            this.parent = parent;
            keepWorldPos = true;
        }


        public void SetReferences()
        {
        }


        public JSerializable GetCopy()
        {
            Transform copyTrans = new Transform()
            {
                tag = this.tag,
                //_parent = this._parent,
                _localPosition = this._localPosition,
                _localRotation = this._localRotation,
                _localScale = this._localScale,
                _localEulerAngles = this._localEulerAngles,
                enabled = this.enabled
            };
            copyTrans.SetParent(this._parent, false);
            copyTrans.RecalculateTRS();

            return copyTrans;
        }

        /*
        public Matrix4x4 WorldMatrix
        {
            get
            {
                Matrix4x4 worldMat = localMatrix;
                if (_parent != null)
                    worldMat *= _parent.WorldMatrix;

                Matrix4x4.Decompose(worldMat, out _worldScale, out _worldRotation, out _worldPosition);
                return worldMat;
            }
        }
        */
        #endregion
    }
}
