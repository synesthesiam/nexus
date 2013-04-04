/*
 *  macserial.h
 *  macserial
 *
 *  Created by Michael Hansen on 9/2/08.
 *  Copyright 2008 __MyCompanyName__. All rights reserved.
 *
 */

#ifndef macserial_
#define macserial_

/* The classes below are exported */
#pragma GCC visibility push(default)

extern "C" {

int openserial(char* device);
unsigned char readserial(int fd);
void settimeout(int fd, int timeout);
void closeserial(int fd);

}

#pragma GCC visibility pop
#endif
