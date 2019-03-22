using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace jail.Models
{
    public class TimeUserInfo
    {
        public long UserId { get; set; }
        public string Name { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }
    
    public enum CheckTypes
    {
        I,
        O
    }

    public class CheckInOut
    {
        public long UserId { get; set; }
        public DateTime CheckTime { get; set; }
        public CheckTypes CheckType { get; set; }
        public int VerifyCode { get; set; }
        public int WorkCode { get; set; }
    }

    public class CheckItem
    {
        public long UserId { get; set; }

        [DisplayFormat(DataFormatString="{0:ddd dd MMM}", ApplyFormatInEditMode = true)]
        public DateTime Date
        {
            get { return Items.Values[0].CheckTime.Date; }
        }

        [DisplayFormat(DataFormatString="{0:hh\\:mm\\:ss}", ApplyFormatInEditMode = true)]
        [DisplayName("In Time")]
        public TimeSpan CheckInTime
        {
            get { return Items.Values[0].CheckTime.TimeOfDay; }
        }
        
        [DisplayFormat(DataFormatString="{0:hh\\:mm\\:ss}", ApplyFormatInEditMode = true)]
        [DisplayName("Out Time")]
        public TimeSpan CheckOutTime
        {
            get { return Items.Count == 1 ? DateTime.Now.TimeOfDay : Items.Values[Items.Count - 1].CheckTime.TimeOfDay; }
        }

        [DisplayFormat(DataFormatString="{0:hh\\:mm\\:ss}", ApplyFormatInEditMode = true)]
        public TimeSpan Duration
        {
            get { return CheckOutTime.Subtract(CheckInTime); }
        }

        public SortedList<DateTime, CheckInOut> Items { get; set; }

        public CheckItem(CheckInOut subItem)
        {
            Items = new SortedList<DateTime, CheckInOut> {{subItem.CheckTime, subItem}};
        }
    }
}