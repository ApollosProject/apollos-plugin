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
    [DisplayName("Apollos App Setup Info")]
    [Category("Apollos App")]
    [Description("A block to help debug common issues with Rock/Apollos App Set")]
    public partial class ApollosAudit : Rock.Web.UI.RockBlock
    {
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            if (!Page.IsPostBack)
            {
                RestAction getCurrentUserAction = new RestActionService(new RockContext()).Queryable().Where(a => a.Path == "api/People/GetCurrentPerson").First();
                bool isUnBlocked = getCurrentUserAction.IsAuthorized(Authorization.VIEW, null);
                if (isUnBlocked)
                {
                    MarkAuthUnBlocked();
                }
                else
                {
                    MarkAuthBlocked(getCurrentUserAction);
                }
            }
        }

        protected void btnEnableGetCurrentPerson_Click(object sender, EventArgs e)
        {
            RestAction getCurrentUserAction = new RestActionService(new RockContext()).Queryable().Where(a => a.Path == "api/People/GetCurrentPerson").First();
            Rock.Security.Authorization.AllowAllUsers(getCurrentUserAction, Authorization.VIEW);
            MarkAuthUnBlocked();
        }

        private void MarkAuthBlocked(RestAction getCurrentUserAction)
        {
            GetCurrentPersonEnabled.Text =  "Auth Blocked";
            GetCurrentPersonEnabled.NotificationBoxType = NotificationBoxType.Danger;
            enableGetCurrentPerson.Visible = true && getCurrentUserAction.IsAuthorized(Authorization.EDIT, this.CurrentPerson);
        }

        private void MarkAuthUnBlocked() {
            GetCurrentPersonEnabled.Text ="Auth Unblocked";
            GetCurrentPersonEnabled.NotificationBoxType = NotificationBoxType.Success;
            enableGetCurrentPerson.Visible = false;
        }
    }
}
