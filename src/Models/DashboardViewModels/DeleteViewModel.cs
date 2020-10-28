﻿using System;

namespace Aiursoft.Colossus.Models.DashboardViewModels
{
    public class DeleteViewModel : LayoutViewModel
    {
        [Obsolete(message: "This method is only for framework", error: true)]
        public DeleteViewModel() { }
        public DeleteViewModel(ColossusUser user) : base(user, "Delete site")
        {

        }

        public void Recover(ColossusUser user)
        {
            RootRecover(user, "Delete site");
        }

        public string SiteName { get; set; }
    }
}
