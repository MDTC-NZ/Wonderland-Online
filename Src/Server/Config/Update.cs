using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Server.Config
{
    public enum UpdtSetting
    {
        Never,
        Auto,
        AutoandForce,
    }

    public enum WarningType
    {
        Message = 0,
        Warning = 1,
        Error = 2,
    }

    public class UpdateSetting : INotifyPropertyChanged
    {
        private bool _enableSchedUpdate;
        private bool _warnofUpdate;
        private UpdtSetting _updtControl;
        private TimeSpan _updtChk_Interval;
        private TimeSpan _autoUpdt_Schedule;
        private int _updateDay;
        private int _updateHour;
        private int _updateMinute;
        private WarningType _warningType;

        public bool EnableSchedUpdate 
        { 
            get { return _enableSchedUpdate; } 
            set { SetField(ref _enableSchedUpdate, value); } 
        }

        public bool WarnofUpdate 
        { 
            get { return _warnofUpdate; } 
            set { SetField(ref _warnofUpdate, value); } 
        }

        public UpdtSetting UpdtControl 
        { 
            get { return _updtControl; } 
            set { SetField(ref _updtControl, value); } 
        }

        public TimeSpan UpdtChk_Interval 
        { 
            get { return _updtChk_Interval; } 
            set { SetField(ref _updtChk_Interval, value); } 
        }

        public TimeSpan AutoUpdt_Schedule 
        { 
            get { return _autoUpdt_Schedule; } 
            set { SetField(ref _autoUpdt_Schedule, value); } 
        }

        public int UpdateDay
        {
            get { return _updateDay; }
            set { SetField(ref _updateDay, value); }
        }

        public int UpdateHour
        {
            get { return _updateHour; }
            set { SetField(ref _updateHour, value); }
        }

        public int UpdateMinute
        {
            get { return _updateMinute; }
            set { SetField(ref _updateMinute, value); }
        }

        public WarningType WarningType
        {
            get { return _warningType; }
            set { SetField(ref _warningType, value); }
        }

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
        #endregion
    }
}
