// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MappingListPanelControl.Designer.cs" company="RHEA System S.A.">
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
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Windows.Forms;
    using System.Windows.Forms.Integration;

    using Autofac;

    using DEHEASysML.ViewModel.Interfaces;
    using DEHEASysML.Views;

    using DEHPCommon;

    using ReactiveUI;

    using DevExpress.Xpf.Core;

    /// <summary>
    /// Interaction logic for the <see cref="MappingListPanelControl" />
    /// </summary>
    [ExcludeFromCodeCoverage]
    partial class MappingListPanelControl
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// The <see cref="ElementHost"/>
        /// </summary>
        private ElementHost mappingViewPanelHost;

        /// <summary>
        /// The <see cref="IMappingListPanelViewModel"/>
        /// </summary>
        private IMappingListPanelViewModel mappingListPanelViewModel;

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
            Cursor.Current = Cursors.WaitCursor;
            this.mappingViewPanelHost = new System.Windows.Forms.Integration.ElementHost();
            this.SuspendLayout();
            // 
            // mappingViewPanelHost
            // 
            this.mappingViewPanelHost.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mappingViewPanelHost.Location = new System.Drawing.Point(0, 0);
            this.mappingViewPanelHost.Name = "mappingViewPanelHost";
            this.mappingViewPanelHost.Size = new System.Drawing.Size(552, 343);
            this.mappingViewPanelHost.TabIndex = 0;
            this.mappingViewPanelHost.Text = "mappingViewPanelHost";
            var mappingListPanel = new MappingListPanel();
            this.mappingListPanelViewModel = AppContainer.Container.Resolve<IMappingListPanelViewModel>();
            mappingListPanel.DataContext = this.mappingListPanelViewModel;
            this.mappingViewPanelHost.Child = mappingListPanel;
            // 
            // HubPanelControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.mappingViewPanelHost);
            this.Name = "MappingListPanelControl";
            Cursor.Current = Cursors.Default;
            this.WhenAnyValue(x => x.mappingListPanelViewModel.IsBusy).Subscribe(this.SetCursor);
            ThemeManager.SetTheme(mappingListPanel, Theme.Default);
            this.ResumeLayout(false);
        }

        #endregion

        /// <summary>
        /// Sets the <see cref="Cursor"/> based on the value
        /// </summary>
        /// <param name="isBusy"></param>
        public void SetCursor(bool? isBusy)
        {
            if (isBusy.HasValue)
            {
                var isBusyValue = isBusy.Value;
                Cursor.Current = isBusyValue ? Cursors.WaitCursor : Cursors.Default;
            }
        }
    }
}
