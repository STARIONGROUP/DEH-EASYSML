// --------------------------------------------------------------------------------------------------------------------
// <copyright file="HubPanelControl.Designer.cs" company="RHEA System S.A.">
// Copyright (c) 2020-2022 RHEA System S.A.
// 
// Author: Sam Gerené, Alex Vorobiev, Alexander van Delft, Nathanael Smiechowski, Antoine Théate.
// 
// This file is part of DEHEASysML
// 
// The DEHEASysML is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 3 of the License, or (at your option) any later version.
// 
// The DEHEASysML is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
// Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public License
// along with this program; if not, write to the Free Software Foundation,
// Inc., 51 Franklin Street, Fifth Floor, Boston, MA 02110-1301, USA.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace DEHEASysML.Forms
{
    using System.Diagnostics.CodeAnalysis;
    using System.Drawing;
    using System.Windows.Forms.Integration;

    using Autofac;
    using DEHEASysML.ViewModel;
    using DEHEASysML.ViewModel.Interfaces;
    using DEHEASysML.Views;

    using DEHPCommon;

    /// <summary>
    /// Interaction logic for the <see cref="HubPanelControl" />
    /// </summary>
    [ExcludeFromCodeCoverage]
    partial class HubPanelControl
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// The <see cref="ElementHost"/>
        /// </summary>
        private ElementHost hubPanelHost;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.hubPanelHost = new System.Windows.Forms.Integration.ElementHost();
            this.SuspendLayout();
            // 
            // hubPanelHost
            // 
            this.hubPanelHost.Dock = System.Windows.Forms.DockStyle.Fill;
            this.hubPanelHost.Location = new System.Drawing.Point(0, 0);
            this.hubPanelHost.Name = "hubPanelHost";
            this.hubPanelHost.Size = new System.Drawing.Size(552, 343);
            this.hubPanelHost.TabIndex = 0;
            this.hubPanelHost.Text = "hubPanelHost";
            var hubPanel = new HubPanel();
            var hubPanelViewModel = AppContainer.Container.Resolve<IHubPanelViewModel>();
            hubPanel.DataContext = hubPanelViewModel;
            this.hubPanelHost.Child = hubPanel;
            // 
            // HubPanelControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.hubPanelHost);
            this.Name = "HubPanelControl";
            this.ResumeLayout(false);
        }

        #endregion
    }
}
