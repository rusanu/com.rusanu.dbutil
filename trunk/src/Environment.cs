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
using System.Data.SqlClient;

namespace com.rusanu.DBUtil
{
    /// <summary>
    /// Execution environment. 
    /// This class contains the current batch execution environment:
    ///  - variable values
    ///  - SQL connection
    ///  - results
    /// </summary>
    public class Environment
    {
        private Dictionary<string, string> _variables;

        /// <summary>
        /// Execution variable values
        /// </summary>
        public Dictionary<string, string> Variables
        {
            get { return _variables; }
        }

        /// <summary>
        /// The SqlConnection used to execute the SQL batches.
        /// </summary>
        public SqlConnection Connection
        {
            get;
            set;
        }

        internal Environment()
        {
            _variables = new Dictionary<string, string>();
        }
    }
}
