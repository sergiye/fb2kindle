using System;
using System.Collections.Generic;
using System.ComponentModel;

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

        [DisplayName("Date")]
//        [DisplayFormat(DataFormatString="{0:dd MMM}", ApplyFormatInEditMode = true)]
        public DateTime Date
        {
            get { return Items.Values[0].CheckTime.Date; }
        }

//        [DisplayFormat(DataFormatString="{0:hh\\:mm\\:ss}", ApplyFormatInEditMode = true)]
        [DisplayName("In Time")]
        public TimeSpan CheckInTime
        {
            get { return Items.Values[0].CheckTime.TimeOfDay; }
        }
        
//        [DisplayFormat(DataFormatString="{0:hh\\:mm\\:ss}", ApplyFormatInEditMode = true)]
        [DisplayName("Out Time")]
        public TimeSpan CheckOutTime
        {
            get { return Items.Count == 1 ? DateTime.Now.TimeOfDay : Items.Values[Items.Count - 1].CheckTime.TimeOfDay; }
        }

//        [DisplayFormat(DataFormatString="{0:hh\\:mm\\:ss}", ApplyFormatInEditMode = true)]
        public TimeSpan Duration
        {
            get { return CheckOutTime.Subtract(CheckInTime); }
        }

        public SortedList<DateTime, CheckInOut> Items { get; set; }

        public CheckItem(CheckInOut subItem)
        {
            Items = new SortedList<DateTime, CheckInOut> {{subItem.CheckTime, subItem}};
        }

        public static List<CheckItem> FromUserCheckData(List<CheckInOut> data)
        {
            var result = new List<CheckItem>();
            foreach (var rec in data)
            {
                var item = result.Find(r => r.Date.Equals(rec.CheckTime.Date));
                if (item != null)
                {
                    item.Items.Add(rec.CheckTime, rec);
                }
                else
                {
                    result.Add(new CheckItem(rec));
                }
            }
            return result;
        }
    }
}