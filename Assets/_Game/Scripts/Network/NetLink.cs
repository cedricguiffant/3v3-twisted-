using System.Collections.Concurrent;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Twisted3v3.Net
{
    /// <summary>
    /// Une connexion TCP framée en lignes UTF-8. Un thread lecteur remplit
    /// <see cref="Inbox"/>, un thread écrivain vide la file d'envoi : le thread
    /// principal Unity ne bloque jamais sur le réseau. Fermer d'un côté réveille
    /// les deux threads proprement.
    /// </summary>
    public sealed class NetLink
    {
        private readonly TcpClient _tcp;
        private readonly StreamReader _reader;
        private readonly StreamWriter _writer;
        private readonly BlockingCollection<string> _outbox = new();
        private volatile bool _closed;

        /// <summary>Messages reçus, à pomper depuis le thread principal.</summary>
        public readonly ConcurrentQueue<string> Inbox = new();

        public int Id;
        public bool IsClosed => _closed;

        public NetLink(TcpClient tcp)
        {
            _tcp = tcp;
            _tcp.NoDelay = true; // latence avant débit (petits messages fréquents)
            var stream = tcp.GetStream();
            _reader = new StreamReader(stream, new UTF8Encoding(false));
            _writer = new StreamWriter(stream, new UTF8Encoding(false)) { AutoFlush = true };
            new Thread(ReadLoop) { IsBackground = true, Name = "NetLink.Read" }.Start();
            new Thread(WriteLoop) { IsBackground = true, Name = "NetLink.Write" }.Start();
        }

        public void Send(string line)
        {
            if (_closed) return;
            try { _outbox.TryAdd(line); }
            catch { /* file complétée pendant la fermeture */ }
        }

        private void ReadLoop()
        {
            try
            {
                string line;
                while ((line = _reader.ReadLine()) != null)
                    Inbox.Enqueue(line);
            }
            catch { /* connexion coupée */ }
            finally { Close(); }
        }

        private void WriteLoop()
        {
            try
            {
                foreach (var line in _outbox.GetConsumingEnumerable())
                    _writer.WriteLine(line);
            }
            catch { /* connexion coupée */ }
            finally { Close(); }
        }

        public void Close()
        {
            if (_closed) return;
            _closed = true;
            try { _outbox.CompleteAdding(); } catch { }
            try { _tcp.Close(); } catch { }
        }
    }
}
