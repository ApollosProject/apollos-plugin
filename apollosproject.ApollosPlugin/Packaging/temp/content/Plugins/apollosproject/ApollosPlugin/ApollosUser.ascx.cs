using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Rock;
using Rock.Model;
using Rock.Data;
using Rock.Web.Cache;
using Rock.Web.UI.Controls;
using Rock.Security;

namespace RockWeb.Plugins.apollosproject.ApollosPlugin
{
    [DisplayName("Apollos App User Configuration")]
    [Category("Apollos App")]
    [Description("A block to help setup the Apollos API user")]
    public partial class ApollosAudit : Rock.Web.UI.RockBlock
    {
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            if (!Page.IsPostBack)
            {
                BindData();

            }
        }

        protected void BindData()
        {
            var rockContext = new RockContext();
            if (!IsUserAuthorized(Authorization.ADMINISTRATE))
            {
                errorMessage.Visible = true;
                errorMessage.Text = "You are not authorized to make changes to the Apollos API key/user.";
                ShowApiKey.Enabled = false;
            } else
            {
                errorMessage.Visible = false;
            }
            var restUserRecordTypeId = DefinedValueCache.Get(Rock.SystemGuid.DefinedValue.PERSON_RECORD_TYPE_RESTUSER.AsGuid()).Id;
            var activeRecordStatusValueId = DefinedValueCache.Get(Rock.SystemGuid.DefinedValue.PERSON_RECORD_STATUS_ACTIVE.AsGuid()).Id;
            var apollosApiUser = new PersonService(rockContext).Queryable()
                .Where(q => q.RecordTypeValueId == restUserRecordTypeId && q.RecordStatusValueId == activeRecordStatusValueId && q.LastName == "apollos").FirstOrDefault();
            if (apollosApiUser == null)
            {
                noUser.Visible = true;
                userExists.Visible = false;

            } else
            {
                noUser.Visible = false;
                userExists.Visible = true;
                var userLogin = new UserLoginService(rockContext).Queryable().Where(u => u.PersonId == apollosApiUser.Id).First();
                apiKey.Text = userLogin.ApiKey;
                apiKey.Visible = false;
            }
        }

        protected void btnCreateApollosApiUserClick(object sender, EventArgs e)
        {
            CreateUser();
            BindData();
        }

        protected void btnShowApiKey(object sender, EventArgs e)
        {
            if (!IsUserAuthorized(Authorization.ADMINISTRATE))
            {
                return;
            }
            ShowApiKey.Visible = false;
            apiKey.Visible = true;
        }

        protected void CreateUser()
        {
            var rockContext = new RockContext();
            var userLoginService = new UserLoginService(rockContext);
            if (!IsUserAuthorized(Authorization.ADMINISTRATE))
            {
                return;
            }
            rockContext.WrapTransaction(() =>
            {
                var personService = new PersonService(rockContext);
                var restUser = new Person();

                    personService.Add(restUser);
                    rockContext.SaveChanges();

                // the rest user name gets saved as the last name on a person
                restUser.LastName = "apollos";
                restUser.RecordTypeValueId = DefinedValueCache.Get(Rock.SystemGuid.DefinedValue.PERSON_RECORD_TYPE_RESTUSER.AsGuid()).Id;
                restUser.RecordStatusValueId = DefinedValueCache.Get(Rock.SystemGuid.DefinedValue.PERSON_RECORD_STATUS_ACTIVE.AsGuid()).Id;
 

                if (restUser.IsValid)
                {
                    rockContext.SaveChanges();
                }

                // the description gets saved as a system note for the person
                var noteType = NoteTypeCache.Get(Rock.SystemGuid.NoteType.PERSON_TIMELINE_NOTE.AsGuid());
                if (noteType != null)
                {
                    var noteService = new NoteService(rockContext);
                    var note = noteService.Get(noteType.Id, restUser.Id).FirstOrDefault();
                    if (note == null)
                    {
                        note = new Note();
                        noteService.Add(note);
                    }
                    note.NoteTypeId = noteType.Id;
                    note.EntityId = restUser.Id;
                    note.Text = "Apollos API User - used for authenticating Apollos Server with Rock";
                }
                rockContext.SaveChanges();

                // the key gets saved in the api key field of a user login (which you have to create if needed)
                var entityType = new EntityTypeService(rockContext)
                    .Get("Rock.Security.Authentication.Database");

                 var userLogin = new UserLogin();
                 userLoginService.Add(userLogin);

                userLogin.UserName = Guid.NewGuid().ToString();

                userLogin.IsConfirmed = true;
                userLogin.ApiKey = GenerateKey();
                userLogin.PersonId = restUser.Id;
                userLogin.EntityTypeId = entityType.Id;
                rockContext.SaveChanges();

                var groupMemberService = new GroupMemberService(rockContext);
                var groupMember = new GroupMember();
                groupMember.PersonId = restUser.Id;
                var adminGroup = new GroupService(rockContext).Get(Rock.SystemGuid.Group.GROUP_ADMINISTRATORS.AsGuid());
                groupMember.GroupId = adminGroup.Id;
                var securityGroupMember = adminGroup.GroupType.Roles.Where(r => r.Guid.Equals(Rock.SystemGuid.GroupRole.GROUPROLE_SECURITY_GROUP_MEMBER.AsGuid())).First();
                groupMember.GroupRoleId = securityGroupMember.Id;
                groupMember.GroupMemberStatus = GroupMemberStatus.Active;
                if (groupMember.IsValid)
                {
                    groupMemberService.Add(groupMember);
                    rockContext.SaveChanges();
                }
            });
        }
        private string GenerateKey()
        {
            StringBuilder sb = new StringBuilder();
            Random rnd = new Random();
            char[] codeCharacters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789".ToCharArray(); ;
            int poolSize = codeCharacters.Length;

            for (int i = 0; i < 24; i++)
            {
                sb.Append(codeCharacters[rnd.Next(poolSize)]);
            }

            return sb.ToString();
        }
    }
}
