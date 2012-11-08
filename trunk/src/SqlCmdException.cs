﻿// Copyright (c) 2010-2012. Rusanu Consulting LLC  
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
 using System;
using System.Collections.Generic;
using System.Text;
using com.rusanu.DBUtil.Properties;

namespace com.rusanu.DBUtil
{
    /// <summary>
    /// This is the exception class raised by the SqlCmd funcitonality
    /// </summary>
    public abstract class SqlCmdException: Exception
    {
        internal SqlCmdException (string format, params object[] args)
            : base (String.Format(format, args))
        {
        }
    }

    public class SqlCmdConnectSyntaxException : SqlCmdException
    {
        public string Command { get; set; }

        public SqlCmdConnectSyntaxException(
            string commandLine)
           : base (Resources.CONNECTSYNTAXEXCEPTIONFORMAT, commandLine)
        {
            Command = commandLine;
        }
    }


    public class SqlCmdSetVarSyntaxException : SqlCmdException
    {
        public string Command { get; set; }

        public SqlCmdSetVarSyntaxException(
            string commandLine)
            : base(Resources.SETVARSYNTAXEXCEPTIONFORMAT, commandLine)
        {
            Command = commandLine;
        }
    }

    public class SqlCmdShellSyntaxException : SqlCmdException
    {
        public string Command { get; set; }

        public SqlCmdShellSyntaxException(
            string commandLine)
            : base (Resources.SHELLSYNTAXEXCEPTIONFORMAT, commandLine)
        {
            Command = commandLine;
        }
    }
}
