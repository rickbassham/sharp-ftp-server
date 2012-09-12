using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace SharpServer
{
    [Serializable]
    public class FtpUser
    {
        [XmlAttribute("username")]
        public string UserName { get; set; }

        [XmlAttribute("password")]
        public string Password { get; set; }

        [XmlAttribute("homedir")]
        public string HomeDir { get; set; }

        [XmlAttribute("twofactorsecret")]
        public string TwoFactorSecret { get; set; }

        [XmlIgnore]
        public bool IsAnonymous { get; set; }
    }

    [Obsolete("This is not a real user store. It is just a stand-in for testing. DO NOT USE IN PRODUCTION CODE.")]
    public static class FtpUserStore
    {
        private static List<FtpUser> _users;

        static FtpUserStore()
        {
            _users = new List<FtpUser>();

            XmlSerializer serializer = new XmlSerializer(_users.GetType(), new XmlRootAttribute("Users"));

            if (File.Exists("users.xml"))
            {
                _users = serializer.Deserialize(new StreamReader("users.xml")) as List<FtpUser>;
            }
            else
            {
                _users.Add(new FtpUser
                {
                    UserName = "rick",
                    Password = "test",
                    HomeDir = "C:\\Utils"
                });

                using (StreamWriter w = new StreamWriter("users.xml"))
                {
                    serializer.Serialize(w, _users);
                }
            }
        }

        public static FtpUser Validate(string username, string password)
        {
            FtpUser user = (from u in _users where u.UserName == username && u.Password == password select u).SingleOrDefault();

            if (user == null)
            {
                user = new FtpUser
                {
                    UserName = username,
                    HomeDir = "C:\\Utils",
                    IsAnonymous = true
                };
            }

            return user;
        }


        public static FtpUser Validate(string username, string password, string twoFactorCode)
        {
            FtpUser user = (from u in _users where u.UserName == username && u.Password == password select u).SingleOrDefault();

            if (TwoFactor.TimeBasedOneTimePassword.IsValid(user.TwoFactorSecret, twoFactorCode))
            {
                return user;
            }

            return null;
        }
    }
}
