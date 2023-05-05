using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ABEngine.ABEditor
{
	public class EditorActionStack
	{
        private Stack<EditorAction> undoStack = new Stack<EditorAction>();
        private Stack<EditorAction> redoStack = new Stack<EditorAction>();

        public void Do(EditorAction action)
        {
            action.Do();
            undoStack.Push(action);
            redoStack.Clear();
        }

        public void Undo()
        {
            if (undoStack.Count > 0)
            {
                EditorAction action = undoStack.Pop();
                action.Undo();
                redoStack.Push(action);
            }
        }

        public void Redo()
        {
            if (redoStack.Count > 0)
            {
                EditorAction action = redoStack.Pop();
                action.Do();
                undoStack.Push(action);
            }
        }

        public T UpdateProperty<T>(T oldValue, T newValue, object targetObject, string memberName, Action<T> setter = null)
        {
            if (!EqualityComparer<T>.Default.Equals(oldValue, newValue))
            {
                //PropertyInfo propertyInfo = targetObject.GetType().GetProperty(propertyName);
                MemberInfo memberInfo = targetObject.GetType().GetMember(memberName, BindingFlags.Public | BindingFlags.Instance).FirstOrDefault();

                OnPropertyChange<T>(targetObject, memberInfo, oldValue, newValue, setter);
                return newValue;
            }

            return oldValue;
        }

        void OnPropertyChange<T>(object targetObject, MemberInfo member, T oldValue, T newValue, Action<T> setter = null)
        {
            var action = new PropertyChangeAction<T>(targetObject, member, oldValue, newValue, setter);
            this.Do(action);
        }
    }
}

