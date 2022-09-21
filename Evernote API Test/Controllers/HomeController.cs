using AsyncOAuth.Evernote.Simple;
using Evernote.EDAM.NoteStore;
using Evernote.EDAM.Type;
using Evernote_API_Test.DAL;
using Evernote_API_Test.Models;
using Microsoft.AspNet.Identity;
using System;
using System.Configuration;
using System.Data.Entity;
using System.Web.Mvc;
using Thrift.Protocol;
using Thrift.Transport;

namespace Evernote_API_Test.Controllers
{
    public class HomeController : Controller
    {
        private EvernoteAuthorizer EvernoteAuthorizer = new EvernoteAuthorizer(ConfigurationManager.AppSettings["Evernote.Url"], ConfigurationManager.AppSettings["Evernote.Key"], ConfigurationManager.AppSettings["Evernote.Secret"]);
        private TestContext db = new TestContext();

        public ActionResult Authorize(bool reauth = false)
        {
            string currentUserID = User.Identity.GetUserId();
            UserSession us = db.UserSessions.Find(currentUserID);

            //check if evernote is already validated with website. use credentials stored in database if they are there.
            if (us != null)
            {
                string noteStoreUrl = us.ENCredentials.NotebookUrl;
                THttpClient noteStoreTransport = new THttpClient(new Uri(noteStoreUrl));
                TBinaryProtocol noteStoreProtocol = new TBinaryProtocol(noteStoreTransport);
                NoteStore.Client noteStore = new NoteStore.Client(noteStoreProtocol);

                SessionHelper.EvernoteCredentials = us.ENCredentials;

                //check to see if user revoked credentials from evernote. clear the credentials if this is so.
                try
                {
                    var mi = noteStore.getSyncState(us.ENCredentials.AuthToken);
                }
                catch (Evernote.EDAM.Error.EDAMUserException ex)
                {
                    if (ex.ErrorCode.ToString() == "AUTH_EXPIRED")
                    {
                        //UserSession userToRemove = new UserSession { UserID = currentUserID };
                        //db.Entry(userToRemove).State = EntityState.Deleted;

                        UserSession userToRemove = db.UserSessions.Find(currentUserID);
                        db.UserSessions.Remove(userToRemove);

                        db.SaveChanges();
                        
                        SessionHelper.Clear();                    
                    }
                }    
            }

            // Allow for reauth
            if (reauth)
                SessionHelper.Clear();

            // First of all, check to see if the user is already registered, in which case tell them that
            if (SessionHelper.EvernoteCredentials != null)
            {
                //return Redirect(Url.Action("AlreadyAuthorized"));
                return Redirect(Url.Action("About"));
            }                

            // Evernote will redirect the user to this URL once they have authorized your application
            String callBackUrl = Request.Url.GetLeftPart(UriPartial.Authority) + Url.Action("ObtainTokenCredentials");

            // Generate a request token - this needs to be persisted till the callback
            AsyncOAuth.RequestToken requestToken = EvernoteAuthorizer.GetRequestToken(callBackUrl);

            // Persist the token
            SessionHelper.RequestToken = requestToken;

            // Redirect the user to Evernote so they can authorize the app
            var callForwardUrl = EvernoteAuthorizer.BuildAuthorizeUrl(requestToken);
            return Redirect(callForwardUrl);
        }

        public ActionResult ObtainTokenCredentials(string oauth_verifier)
        {
            // Use the verifier to get all the user details we need and
            // store them in EvernoteCredentials
            EvernoteCredentials credentials = EvernoteAuthorizer.ParseAccessToken(oauth_verifier, SessionHelper.RequestToken);
            if (credentials != null)
            {
                SessionHelper.EvernoteCredentials = credentials;

                UserSession us = new UserSession();

                us.UserID = User.Identity.GetUserId();
                us.ENCredentials = credentials;
                us.DateTimeCreated = DateTime.Now;
                db.UserSessions.Add(us);
                db.SaveChanges();                

                //return Redirect(Url.Action("Authorized"));
                return Redirect(Url.Action("About"));
            }
            else
            {
                return Redirect(Url.Action("Unauthorized"));
            }
        }

        public ActionResult Authorized()
        {
            return View(SessionHelper.EvernoteCredentials);
        }

        public ActionResult Unauthorized()
        {
            return View();
        }

        public ActionResult AlreadyAuthorized()
        {
            return View(SessionHelper.EvernoteCredentials);
        }

        [AllowAnonymous]
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            EvernoteCredentials credentials = SessionHelper.EvernoteCredentials;

            string noteStoreUrl = credentials.NotebookUrl;
            THttpClient noteStoreTransport = new THttpClient(new Uri(noteStoreUrl));
            TBinaryProtocol noteStoreProtocol = new TBinaryProtocol(noteStoreTransport);
            NoteStore.Client noteStore = new NoteStore.Client(noteStoreProtocol);

            Note myNote = new Note();
            myNote.Title = "Chocolate Milk - " + DateTime.Now;
            myNote.Content = @"<?xml version=""1.0"" encoding=""UTF-8""?>
                <!DOCTYPE en-note SYSTEM ""http://xml.evernote.com/pub/enml2.dtd"">
                <en-note>All the chocolate milk.</en-note>";
            
            noteStore.createNote(credentials.AuthToken, myNote);
 
            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";
            //Authorize();
            //return View();
            return Redirect(Url.Action("Authorize"));
        }
    }
}