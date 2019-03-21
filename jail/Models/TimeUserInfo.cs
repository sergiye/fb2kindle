using System;

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
}