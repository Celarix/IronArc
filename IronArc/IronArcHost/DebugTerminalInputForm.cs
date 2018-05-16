﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace IronArcHost
{
	public partial class DebugTerminalInputForm : Form
	{
		public enum DebugTerminalInputType
		{
			Character,
			Line
		}

		private DebugTerminalInputType inputType;
		public string Input => TextInput.Text;

		public DebugTerminalInputForm(DebugTerminalInputType inputType, Guid machineID)
		{
			InitializeComponent();

			this.inputType = inputType;
			if (inputType == DebugTerminalInputType.Character)
			{
				StaticLabelDescription.Text = $"VM {machineID.ToString()} requires a character of input.";
			}
			else
			{
				StaticLabelDescription.Text = $"VM {machineID.ToString()} requires a line of input.";
			}
		}

		private void TextInput_TextChanged(object sender, EventArgs e)
		{
			ButtonSubmit.Enabled = TextInput.Text.Any();

			if (inputType == DebugTerminalInputType.Character && TextInput.Text.Length > 1)
			{
				TextInput.Text = TextInput.Text[0].ToString();
			}
		}

		private void ButtonSubmit_Click(object sender, EventArgs e)
		{
			Close();
		}
	}
}