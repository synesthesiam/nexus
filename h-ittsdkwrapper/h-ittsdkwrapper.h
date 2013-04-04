/*
 *  h-ittsdkwrapper.h
 *  h-ittsdkwrapper
 *
 *  Created by Mike on 9/13/08.
 *  Copyright 2008 __MyCompanyName__. All rights reserved.
 *
 */

#ifndef h_ittsdkwrapper_
#define h_ittsdkwrapper_

/* The classes below are exported */
#pragma GCC visibility push(default)

extern "C" {

int inspect(unsigned char* bytes, unsigned int* remote_id, unsigned int* key);

}

#pragma GCC visibility pop
#endif
