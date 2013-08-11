StreamPipe
==========

A .NET stream supporting concurrent reads and writes transparently.

Just create a new StreamPipe(), and a thread can write to it while another one reads.

Writes are blocked if the buffer is full. Reads are blocked if the buffer is empty.

Call method WriteIsFinished() when writing is over. Once the reader has emptied the buffer, it will receive a 
zero-length result signalling the end of the stream.

Have a look a the unit test project to see some example usage scenarios.

