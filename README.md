# EventReducer
This repo grew out of an answer I posted stackoverflow.com "How to reduce frequency of continuously fired event's event handling".

The problem is that if a handler for an event is expected to have a longer execution time than the when the next event occurs (e.g. a handler for the MouseMove event that calculate all prime numbers between 2 and the sum of the X+Y coordinates of the current mouse position), how can one reduce the number of times the event is "handled" so that at most only one of these long running handlers is running at a time.

The original answer I felt could be improved considerably and is now ready for more generalized usage.

Among the improvements are that when a thread is released to execute the handler it will use more recent EventArgs than when it was queued.

Tests and a console app are provided.
