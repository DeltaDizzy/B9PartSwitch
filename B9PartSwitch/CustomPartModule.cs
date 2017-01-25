﻿using UniLinq;
using UnityEngine;
using B9PartSwitch.Fishbones;

namespace B9PartSwitch
{
    public abstract class CustomPartModule : PartModule, ISerializationCallbackReceiver
    {
        #region Fields

        [NodeData(persistent = true)]
        public string moduleID;

        [SerializeField]
        private SerializedDataContainer serializedData;

        #endregion

        #region Setup

        private void Start()
        {
            // Cast to array so that there aren't issues with modifying the enumerable in a loop
            var otherModules = part.Modules.OfType<CustomPartModule>().Where(m => m != this && m.GetType() == this.GetType()).ToArray();
            if (otherModules.Length > 0 && moduleID.IsNullOrEmpty())
            {
                LogError("Must have a moduleID defined if more than one " + this.GetType().Name + " is present on a part.  This module will be removed");
                part.Modules.Remove(this);
                Destroy(this);
                return;
            }

            foreach (var m in otherModules)
            {
                if (m.moduleID.IsNullOrEmpty() || m.moduleID == moduleID)
                {
                    LogError("Two " + GetType().Name + " modules on the same part must have different (and non-empty) moduleID identifiers.  The second " + GetType().Name + " will be removed");
                    part.Modules.Remove(m);
                    Destroy(m);
                }
            }
        }

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);

            if (!moduleID.IsNullOrEmpty() && node.HasValue(nameof(moduleID)))
            {
                string newID = node.GetValue(nameof(moduleID));
                if (!string.Equals(moduleID, newID))
                {
                    var correctModule = part.Modules.OfType<CustomPartModule>().FirstOrDefault(m => m != this && m.GetType() == this.GetType() && m.moduleID == newID);
                    if (correctModule.IsNotNull())
                    {
                        LogWarning("OnLoad was called with the wrong ModuleID ('" + newID + "'), but found the correct module to load");
                        correctModule.Load(node);
                    }
                    else
                    {
                        LogError("OnLoad was called with the wrong ModuleID and the correct module could not be found");
                    }
                    return;
                }
            }

            this.LoadFields(node);
        }

        public override void OnSave(ConfigNode node)
        {
            base.OnSave(node);
            this.SaveFields(node);
        }

        #endregion

        #region Serialization Methods

        public void OnBeforeSerialize()
        {
            serializedData = this.SerializeToContainer();
        }

        public void OnAfterDeserialize()
        {
            this.DeserializeFromContainer(serializedData);

            Destroy(serializedData);
            serializedData = null;
        }

        #endregion

        #region Logging

        protected void LogInfo(object message) => ((PartModule)this).LogInfo(message);

        protected void LogWarning(object message) => ((PartModule)this).LogWarning(message);

        protected void LogError(object message) => ((PartModule)this).LogError(message);

        public override string ToString()
        {
            string log = this.GetType().Name;
            if (!moduleID.IsNullOrEmpty())
                log += " (moduleID='" + moduleID + "')";
            if (part != null)
                log += " on part " + part.name;
            return log;
        }
        #endregion
    }
}