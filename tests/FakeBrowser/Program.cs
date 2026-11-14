// Writes its arguments (one per line, JSON-escaped-free: each arg base64 encoded) to
// %FAKEBROWSER_OUT%\<pid>.txt so integration tests can verify exact argv without quoting ambiguity.
string? dir = Environment.GetEnvironmentVariable("FAKEBROWSER_OUT");
if (string.IsNullOrEmpty(dir)) return 1;
Directory.CreateDirectory(dir);
string marker = Environment.GetEnvironmentVariable("LAZYBLOX_FORWARDED") ?? "";
var lines = new List<string> { "guard:" + marker };
lines.AddRange(args.Select(a => "arg:" + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(a))));
File.WriteAllLines(Path.Combine(dir, Environment.ProcessId + ".txt"), lines);
return 0;
