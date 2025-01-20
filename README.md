# TcpServer
## Light weight and low thread capacity TcpServer

It was an article on habr.com about a good perfomance and "not greedy" tcp server that runs in limited threads. 
The server was written in C++, and I decided to make something similar in C#. 

In the process of studying, I came across an article that discussed the performance of various configurations
in cross-thread interaction. The author claimed that the best performance is achieved when using the System.Threading.Channels. 

The first implementation was based on the combined use of ConcurentQueue and Channel from the package above. 
According to the test results, I received 4852 ms on 1000 requests. You can see how the measurement is performed in TcpClientTest. 

Then there was a new implementation and I've got rid of the queue and used two channels. 
As a result, with the same number of requests, I've got a time of 1623 ms. 
The performance has increased by more than 2 times, the server is running without unnecessary locks. 

After some optimization, it was possible to achieve a time of less than 1000 ms. 
At the same time, 8 threads were running (AMD Ryzen 5 1400 Quad-Core Processor 3.80 GHz (8 Logic Processors))

