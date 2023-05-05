using System;
using System.Reflection;

namespace ABEngine.ABEditor
{
    public class PropertyChangeAction<T> : EditorAction
    {
        private object TargetObject;
        private MemberInfo Property;
        private T OldValue;
        private T NewValue;
        private Action<T> Setter;

        public PropertyChangeAction(object target, MemberInfo property, T oldValue, T newValue, Action<T> setter = null)
        {
            TargetObject = target;
            Property = property;
            OldValue = oldValue;
            NewValue = newValue;
            Setter = setter;
        }

        public override void Do()
        {
            SetValue(NewValue);
        }

        public override void Undo()
        {
            SetValue(OldValue);
        }

        private void SetValue(T value)
        {
            if (Setter != null)
            {
                Setter(value);
            }
            else
            {
                switch (Property)
                {
                    case PropertyInfo property:
                        property.SetValue(TargetObject, value);
                        break;
                    case FieldInfo field:
                        field.SetValue(TargetObject, value);
                        break;
                    default:
                        break;
                }
            }
        }
    }
}

