using AsyncOAuth.Evernote.Simple;
using System;
using System.ComponentModel.DataAnnotations;

namespace Evernote_API_Test.Models
{
    public class UserSession
    {
        [Key]
        public String UserID { get; set; }
        public EvernoteCredentials ENCredentials { get; set; }
        public DateTime DateTimeCreated { get; set; }
    }
}