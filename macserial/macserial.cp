#include <fcntl.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <sys/ioctl.h>
#include <unistd.h>
#include <termios.h>

#include "macserial.h"

extern "C" {

int openserial(char* device)
{
    int fd = open(device, O_RDWR | O_NOCTTY | O_NONBLOCK);
    ioctl(fd, TIOCEXCL);
    fcntl(fd, F_SETFL, 0);
    
    termios options;
    tcgetattr(fd, &options);    
    cfsetspeed(&options, 19200);
    options.c_cflag |= CS8;
    options.c_cflag &= ~(PARENB);
    options.c_cflag &= ~(CSTOPB);
    tcsetattr(fd, TCSANOW, &options);
    
    return (fd);
}

unsigned char readserial(int fd)
{
    unsigned char result;
    read(fd, &result, 1);
    
    return (result);
}

void settimeout(int fd, int timeout)
{
    termios options;
    tcgetattr(fd, &options);    
    options.c_cc[VMIN] = 1;
    options.c_cc[VTIME] = timeout / 100;
    tcsetattr(fd, TCSANOW, &options);
}

void closeserial(int fd)
{
    tcdrain(fd);
    close(fd);
}

}
