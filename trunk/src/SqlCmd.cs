// Copyright (c) 2010-2012. Rusanu Consulting LLC  
// https://github.com/rusanu/DbUtilSqlCmd
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at

//    http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// 

#region Usings

using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

#endregion

namespace com.rusanu.DBUtil {
	/// <summary>
	/// Class for sqlcmd functionality
	/// </summary>
	public class SqlCmd : IDisposable {
		private readonly Environment _environment;
		private SqlConnection _privateConnection;
		private string currentDirectory;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="conn">The SQL Connection used to execute the SQL batches</param>
		public SqlCmd (SqlConnection conn) {
			_environment = new Environment ();
			_environment.Connection = conn;
			BatchDelimiter = "GO";
		}

		/// <summary>
		/// Execution Environment 
		/// </summary>
		public Environment Environment {
			get { return _environment; }
		}

		public string LastBatch { get; private set; }

		public SqlException LastException { get; private set; }

		/// <summary>
		/// The batch delimiter.
		/// </summary>
		public string BatchDelimiter { get; set; }

		/// <summary>
		/// Determines if to continue or break in case of SQL error.
		/// Can be controlled from the SQL file by using `:on error [exit:ignore]`
		/// </summary>
		public bool ContinueOnError { get; set; }

		public void Dispose () {
			if (null != _privateConnection) {
				_privateConnection.Dispose ();
				_privateConnection = null;
			}
		}

		/// <summary>
		/// This event is raised before executing each batch in the file
		/// </summary>
		public event EventHandler<SqlCmdExecutingEventArgs> Executing;

		/// <summary>
		/// Executes a SQL file on the given connection
		/// </summary>
		/// <param name="conn">Connection to execute the file on</param>
		/// <param name="file">The SQL file being executed</param>
		public static void ExecuteFile (
			SqlConnection conn,
			string filePath) {
			var sqlCmd = new SqlCmd (conn);
			sqlCmd.ExecuteFile (filePath);
		}

		/// <summary>
		/// Executes a SQL file
		/// </summary>
		/// <param name="file">The SQL file to execute</param>
		public bool ExecuteFile (
			string filePath) {
			var regDelimiter = new Regex (@"^\b*" + BatchDelimiter + @"\b*(\d*)", RegexOptions.IgnoreCase);
			var regReplacements = new Regex (@"\$\((\w+)\)");
			var regCommands = new Regex (@"^:([\!\w]+)");
			var currentBatch = new StringBuilder ();
			var filesQueue = new Queue<TextReader> ();

			if (string.IsNullOrEmpty (currentDirectory)) {
				currentDirectory = Path.GetDirectoryName (filePath);
				System.Environment.CurrentDirectory = currentDirectory;
			}
			var file = File.OpenText (filePath) as TextReader;

			filesQueue.Enqueue (file);

			string line = null;
			do {
				line = file.ReadLine ();

				MatchCollection delimiterMatches = null;

				if (null != line) {
					delimiterMatches = regDelimiter.Matches (line);
				}

				if (null == line || delimiterMatches.Count > 0) {
					uint count = 1;
					if (null != delimiterMatches) {
						if (2 == delimiterMatches [0].Groups.Count) {
							//count = Convert.ToUInt32(delimiterMatches[0].Groups[1].Value);
						}
					}

					string batch = currentBatch.ToString ();
					if (false == ExecuteBatch (batch, count) &&
						 false == ContinueOnError) {
						return false;
					}
					currentBatch = new StringBuilder ();
					if (null == file) {
						file = filesQueue.Dequeue ();
					}
					continue;
				}

				Debug.Assert (null != line);

				MatchCollection lineReplacements = regReplacements.Matches (line);
				for (int i = lineReplacements.Count; i > 0; --i) {
					Debug.Assert (lineReplacements [i - 1].Captures.Count == 1);
					Capture c = lineReplacements [i - 1].Captures [0];
					string replacement;
					Debug.Assert (c.Value.Length > 3);
					string key = c.Value.Substring (2, c.Value.Length - 3);
					if (Environment.Variables.TryGetValue (key, out replacement)) {
						line = line.Remove (c.Index, c.Length);
						line = line.Insert (c.Index, replacement);
					}
				}

				MatchCollection commandMatches = regCommands.Matches (line);
				if (commandMatches.Count > 0) {
					Debug.Assert (2 == commandMatches [0].Groups.Count);
					string command = commandMatches [0].Groups [1].Value;
					switch (command.ToLower ()) {
						case "list":
						case "reset":
						case "error":
						case "ed":
						case "out":
						case "perftrace":
						case "help":
						case "serverlist":
						case "xml":
						case "listvar":
							Debug.WriteLine (String.Format ("SqlCmd: command not implemented '{0}' in line: {1}'", command, line));
							break;
						case "r":
							RunCommand (line);
							break;
						case "connect":
							ConnectCommand (line);
							break;
						case "on": /*on error*/
							OnErrorCommand (line);
							break;
						case "!!":
							ShellCommand (line);
							break;
						case "quit":
						case "exit":
							return true;
						case "setvar":
							SetVarCommand (line);
							break;
						default:
							Debug.WriteLine (String.Format ("SqlCmd: Unknown command '{0}' in line: {1}", command, line));
							break;
					}
				} else {
					currentBatch.AppendLine (line);
				}
			} while (null != line && filesQueue.Count > 0);
			return true;
		}

		private void RunCommand (string line) {
			Regex regFile = new Regex (@":r\s+(?<file>.+)", RegexOptions.IgnoreCase);
			var match = regFile.Match (line);
			if (!match.Success) {
				return;
			}
			var fileMatch = match.Groups ["file"];
			if (fileMatch == null) {
				return;
			}

			var filePath = fileMatch.Value;
			if (string.IsNullOrEmpty (filePath) || !File.Exists (filePath)) {
				return;
			}

			ExecuteFile (filePath);
		}

		private void ConnectCommand (string line) {
			// server_name[\instance_name] [-l timeout] [-U user_name [-P password]] 
			var regConnect = new Regex (@"^:connect\s+(?<server>[^\s]+)(?:\s+-l\s+(?<timeout>[\d]+))?(?:\s+-U\s+(?<user>[^\s]+))?(?:\s+-P\s+(?<password>[^\s]+))?", RegexOptions.IgnoreCase);
			MatchCollection connectMatches = regConnect.Matches (line);

			if (connectMatches.Count != 1) {
				throw new SqlCmdConnectSyntaxException (line);
			}

			Match m = connectMatches [0];

			var scsb = new SqlConnectionStringBuilder ();

			Group serverGroup = m.Groups ["server"];
			if (false == serverGroup.Success) {
				throw new SqlCmdConnectSyntaxException (line);
			}
			scsb.DataSource = m.Groups ["server"].Value;

			Group timeoutGroup = m.Groups ["timeout"];
			if (timeoutGroup.Success) {
				int timeout = Convert.ToInt32 (timeoutGroup.Value);
				scsb.ConnectTimeout = timeout;
			}

			Group userGroup = m.Groups ["user"];
			if (userGroup.Success) {
				scsb.UserID = userGroup.Value;
				Group passwordGroup = m.Groups ["password"];
				if (passwordGroup.Success) {
					scsb.Password = passwordGroup.Value;
				}
			} else {
				scsb.IntegratedSecurity = true;
			}

			if (null != _privateConnection) {
				_privateConnection.Dispose ();
			}
			_privateConnection = new SqlConnection (scsb.ConnectionString);
			_privateConnection.Open ();
			Environment.Connection = _privateConnection;
		}


		private void OnErrorCommand (string line) { }

		private void SetVarCommand (string line) {
			var regSetVar = new Regex (@"^:setvar\s+(?<name>[\w_-]+)(?:\s+(?<value>[^\s]+))?", RegexOptions.IgnoreCase);
			MatchCollection matchSetVar = regSetVar.Matches (line);
			if (1 != matchSetVar.Count) {
				throw new SqlCmdSetVarSyntaxException (line);
			}

			Match m = matchSetVar [0];

			Group variableGroup = m.Groups ["name"];
			Debug.Assert (variableGroup.Success);

			Group valueGroup = m.Groups ["value"];
			if (valueGroup.Success) {
				Environment.Variables [variableGroup.Value] = valueGroup.Value;
			} else {
				Environment.Variables.Remove (variableGroup.Value);
			}
		}

		private void ShellCommand (string line) {
			var regShell = new Regex (@":!!\s+(?<command>""[^""]+""|[^\s]+)(?:\s+(?<arguments>.+))?", RegexOptions.IgnoreCase);
			MatchCollection matchShell = regShell.Matches (line);

			Debug.Assert (1 == matchShell.Count);
			Match m = matchShell [0];

			Group commandGroup = m.Groups ["command"];
			if (false == commandGroup.Success) {
				throw new SqlCmdShellSyntaxException (line);
			}

			Group argsGroup = m.Groups ["arguments"];

			Process.Start (commandGroup.Value, argsGroup.Value);
		}

		private bool ExecuteBatch (string batch, uint count) {
			if (String.IsNullOrEmpty (batch)) {
				return true;
			}
			while (count > 0) {
				if (null != Executing) {
					var args = new SqlCmdExecutingEventArgs (
						Environment, batch);
					Executing (this, args);
				}
				var cmd = new SqlCommand (batch, Environment.Connection);
				cmd.CommandTimeout = 0;
				try {
					LastBatch = batch;
					cmd.ExecuteNonQuery ();
				} catch (SqlException sqlex) {
					LastException = sqlex;
					if (false == ContinueOnError) {
						return false;
					}
				}
				--count;
			}
			return true;
		}
	}
}