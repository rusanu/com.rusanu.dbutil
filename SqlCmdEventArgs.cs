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

namespace com.rusanu.DBUtil
{
    /// <summary>
    /// This class is the arguments passed to the SqlCmd.Executing event
    /// </summary>
    public class SqlCmdExecutingEventArgs:EventArgs
    {
        /// <summary>
        /// Environment of the SqlCmd that is executing the batch
        /// </summary>
        public Environment Environment { get; private set; }

        /// <summary>
        /// Batch being executing
        /// </summary>
        public string Batch { get; private set; }

        internal SqlCmdExecutingEventArgs(
            Environment environment,
            string batch)
        {
            Environment = environment;
            Batch = batch;
        }
    }
}
