//|--- DiagnosticsJsonSerializer.cs ----------------------------|
//
// This source code file is part of the Iona project.
//
// Copyright (c) 2026 Marcel Kulina
// Licensed under MIT
//
//|-------------------------------------------------------------|

using System.Text;
using System.Text.Json;

namespace Shared
{
    public class DiagnosticsJsonSerializer
    {
        public string Serialize(
            IEnumerable<CompilerError> errors,
            IEnumerable<CompilerWarning> warnings,
            bool indented = true)
        {
            using var stream = new MemoryStream();
            using (var w = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = indented }))
            {
                w.WriteStartObject();
                w.WriteString("schema", "iona-diagnostics/v1");

                w.WriteStartArray("diagnostics");
                foreach (var e in errors)
                    WriteDiagnostic(w, "error", e.Code, e.Message, e.Meta);
                foreach (var warning in warnings)
                    WriteDiagnostic(w, "warning", warning.Code, warning.Message, warning.Meta);
                w.WriteEndArray();

                w.WriteEndObject();
            }

            return Encoding.UTF8.GetString(stream.ToArray());
        }

        private void WriteDiagnostic(Utf8JsonWriter w, string severity, string code, string message, Metadata meta)
        {
            w.WriteStartObject();
            w.WriteString("severity", severity);
            w.WriteString("code", code);
            w.WriteString("message", message);
            w.WriteString("file", meta.File ?? "");
            w.WriteNumber("lineStart", meta.LineStart);
            w.WriteNumber("lineEnd", meta.LineEnd);
            w.WriteNumber("columnStart", meta.ColumnStart);
            w.WriteNumber("columnEnd", meta.ColumnEnd);
            w.WriteEndObject();
        }
    }
}
