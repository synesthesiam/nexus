/*
 *  h-ittsdkwrapper.cp
 *  h-ittsdkwrapper
 *
 *  Created by Mike on 9/13/08.
 *  Copyright 2008 __MyCompanyName__. All rights reserved.
 *
 */

#include <iostream>
#include "h-ittsdkwrapper.h"
#include "H-ITTSDK.h"

extern "C" {

int inspect(unsigned char* bytes, unsigned int* remote_id, unsigned int* key)
{
	return (hitt_inspect(bytes, remote_id, key));
}

}
