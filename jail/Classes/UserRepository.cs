using System;
using System.Collections.Generic;
using jail.Models;
using Simpl.Extensions.Database;
using Simpl.Extensions.Encryption;

namespace jail.Classes
{
    internal class UserRepository
    {
        protected static BaseConnectionProvider<long> Db { get; set; }

        static UserRepository()
        {
            Db = new MsSqlConnectionProvider<long>(SettingsHelper.StatisticDatabase);
        }

        #region Users

        public static UserProfile GetUserById(long id)
        {
            var data = Db.QueryOne<UserProfile>("select * from Users where Id=@id", new { id });
            return data;
        }

        private static UserProfile GetRestoreAdministratorProfile(string email)
        {
            return new UserProfile {Id = 0, Email = email, UserType = UserType.Administrator, TimeTrackId = 29, Active = true, RegisteredTime = DateTime.Now};
        }

        public static UserProfile GetUser(string login)
        {
            if (!string.IsNullOrEmpty(login) && login.GetHash().Equals(CommonHelper.AdminLoginHash))
                return GetRestoreAdministratorProfile(login);
            return Db.QueryOne<UserProfile>("select * from Users where Email like @login", new { login });
        }
        
        public static UserProfile GetUser(string login, string password)
        {
            if (!string.IsNullOrEmpty(password) && !string.IsNullOrEmpty(login) && 
                login.GetHash().Equals(CommonHelper.AdminLoginHash) &&
                password.GetHash().Equals(CommonHelper.AdminPasswordHash))
                return GetRestoreAdministratorProfile(login);

            var result = string.IsNullOrEmpty(password)
                ? Db.QueryOne<UserProfile>("select * from Users where Email like @login and Password is null",
                    new { login })
                : Db.QueryOne<UserProfile>("select * from Users where Email like @login and Password=@password",
                    new { login, password = password.GetHash()});
            return result;
        }

        public static List<UserProfile> GetUsers(string filter)
        {
            var users = Db.Query<UserProfile>("select TOP(100) * from Users where Email like @key order by Email", 
                new { key = "%" + filter + "%" });
            return users;
        }

        public static bool SetUserPassword(long userId, string oldPassword, string newPassword)
        {
            var userFound = string.IsNullOrEmpty(oldPassword)
                ? Db.QueryExists("select Id from Users where Id=@Id and password is null", new { Id = userId })
                : Db.QueryExists("select Id from Users where Id=@Id and password=@password", new {Id = userId, password = oldPassword.GetHash()});
            return userFound && SetUserPassword(userId, newPassword);
        }

        public static bool SetUserPassword(long userId, string newPassword)
        {
            var result = Db.Execute("update Users set Password=@password where Id=@userId", new { password = newPassword == null ? null : newPassword.GetHash(), userId });
            return result == 1;
        }

        public static void DeleteUser(long userId)
        {
            Db.Execute("DELETE from Users where Id=@userId", new { userId });
            Logger.WriteInfo(string.Format("User {0} deleted", userId));
        }

        public static void SaveUser(UserProfile user)
        {
            Db.Save(user);
        }

        public static bool IsUserLoginUnique(string login, long userId)
        {
            return Db.QueryOne<int>("select count(1) from Users where email=@login and Id<>@userId", new { login, userId }) == 0;
        }

        #endregion Users
    }
}