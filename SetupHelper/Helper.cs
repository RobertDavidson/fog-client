using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Linq;

namespace SetupHelper
{
    [RunInstaller(true)]
    public partial class Helper : Installer
    {
        public Helper()
        {
            InitializeComponent();
        }

        public override void Install(IDictionary stateSaver)
        {
            base.Install(stateSaver);

        }
    }
}
