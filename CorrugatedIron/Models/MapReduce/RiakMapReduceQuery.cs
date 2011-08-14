﻿// Copyright (c) 2011 - OJ Reeves & Jeremiah Peschka
//
// This file is provided to you under the Apache License,
// Version 2.0 (the "License"); you may not use this file
// except in compliance with the License.  You may obtain
// a copy of the License at
//
//   http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing,
// software distributed under the License is distributed on an
// "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
// KIND, either express or implied.  See the License for the
// specific language governing permissions and limitations
// under the License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CorrugatedIron.Extensions;
using CorrugatedIron.Models.MapReduce.KeyFilters;
using CorrugatedIron.Messages;
using CorrugatedIron.Models.MapReduce.Fluent;
using CorrugatedIron.Models.MapReduce.Inputs;
using CorrugatedIron.Models.MapReduce.Languages;
using CorrugatedIron.Models.MapReduce.Phases;
using CorrugatedIron.Util;
using Newtonsoft.Json;

namespace CorrugatedIron.Models.MapReduce
{
    public class RiakMapReduceQuery
    {
        private readonly List<RiakPhase> _phases;
        private readonly List<IRiakKeyFilterToken> _filters;
        private string _query;
        private RiakPhaseInputs _inputs;

        public string ContentType { get; set; }

        public RiakMapReduceQuery()
        {
            _phases = new List<RiakPhase>();
            _filters = new List<IRiakKeyFilterToken>();
            ContentType = RiakConstants.ContentTypes.ApplicationJson;
        }

        public RiakMapReduceQuery Inputs(string bucket)
        {
            _inputs = new RiakPhaseInputs(new RiakBucketInput(bucket));
            return this;
        }

        public RiakMapReduceQuery Inputs(IEnumerable<RiakBucketKeyInput> inputs)
        {
            _inputs = new RiakPhaseInputs(inputs);
            return this;
        }

        public RiakMapReduceQuery Inputs(IEnumerable<RiakBucketKeyArgInput> inputs)
        {
            _inputs = new RiakPhaseInputs(inputs);
            return this;
        }

        public RiakMapReduceQuery MapErlang(Action<RiakFluentActionPhaseErlang> setup)
        {
            var phase = new RiakMapPhase<RiakPhaseLanguageErlang>();
            var fluent = new RiakFluentActionPhaseErlang(phase);
            setup(fluent);
            _phases.Add(phase);
            return this;
        }

        public RiakMapReduceQuery MapJs(Action<RiakFluentActionPhaseJavascript> setup)
        {
            var phase = new RiakMapPhase<RiakPhaseLanguageJavascript>();
            var fluent = new RiakFluentActionPhaseJavascript(phase);
            setup(fluent);
            _phases.Add(phase);
            return this;
        }

        public RiakMapReduceQuery ReduceErlang(Action<RiakFluentActionPhaseErlang> setup)
        {
            var phase = new RiakReducePhase<RiakPhaseLanguageErlang>();
            var fluent = new RiakFluentActionPhaseErlang(phase);
            setup(fluent);
            _phases.Add(phase);
            return this;
        }

        public RiakMapReduceQuery ReduceJs(Action<RiakFluentActionPhaseJavascript> setup)
        {
            var phase = new RiakReducePhase<RiakPhaseLanguageJavascript>();
            var fluent = new RiakFluentActionPhaseJavascript(phase);
            setup(fluent);
            _phases.Add(phase);
            return this;
        }

        public RiakMapReduceQuery Link(Action<RiakFluentLinkPhase> setup)
        {
            var phase = new RiakLinkPhase();
            var fluent = new RiakFluentLinkPhase(phase);
            setup(fluent);
            _phases.Add(phase);
            return this;
        }

        public RiakMapReduceQuery Filter(IRiakKeyFilterToken filter)
        {
            _filters.Add(filter);
            return this;
        }

        public void Compile()
        {
            System.Diagnostics.Debug.Assert(_inputs != null);
            if (!string.IsNullOrWhiteSpace(_query)) return;

            var sb = new StringBuilder();

            using (var sw = new StringWriter(sb))
            using(JsonWriter writer = new JsonTextWriter(sw))
            {
                writer.WriteStartObject();

                writer.WritePropertyName("inputs");
                _inputs.WriteJson(writer);

                if (_filters.Count > 0)
                {
                    writer.WritePropertyName("key_filters");
                    writer.WriteStartArray();
                    _filters.ForEach(f => writer.WriteRawValue(f.ToJsonString()));
                    writer.WriteEndArray();
                }

                writer.WritePropertyName("query");
                writer.WriteStartArray();
                _phases.ForEach(p => writer.WriteRawValue(p.ToJsonString()));
                writer.WriteEndArray();

                writer.WriteEndObject();
            }

            _query = sb.ToString();
        }

        internal RpbMapRedReq ToMessage()
        {
            Compile();
            var message = new RpbMapRedReq
                              {
                                  Request = _query.ToRiakString(),
                                  ContentType = ContentType.ToRiakString()
                              };

            return message;
        }
    }
}
