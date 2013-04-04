#include <stdio.h>
#include <time.h>
#include "src/wiic.h"

#define MAX_WIIMOTES 7
#define NS_PER_SEC 1000000000

static wiimote** wiimotes;
static int done = 0;

// ===========================================================================

void empty_button_callback(int id, int buttons) { }

void (*button_callback)(int, int) = empty_button_callback;

void set_callbacks(void (new_button_callback)(int, int)) {
  button_callback = new_button_callback;
}

// ===========================================================================

int wrapper_connect(int timeout) {
  wiimotes = wiic_init(MAX_WIIMOTES);

  int num_found = wiic_find(wiimotes, MAX_WIIMOTES, timeout);
  int i;

  if (num_found > 0) {
    int num_connected = wiic_connect(wiimotes, num_found);

    for (i = 0; i < MAX_WIIMOTES; ++i) {
      if (WIIMOTE_IS_CONNECTED(wiimotes[i])) {
        wiic_set_leds(wiimotes[i], WIIMOTE_LED_1); 
        wiic_rumble(wiimotes[i], 1);
        usleep(500000);
        wiic_rumble(wiimotes[i], 0);
      }
    }

    done = 0;
    return (num_connected);
  }

  return (0);
}

void wrapper_poll() {
  int i;
  struct timespec ts_start, ts_end;
  unsigned long elapsed;

  while (!done) {
    elapsed = 0;
    clock_gettime(CLOCK_REALTIME, &ts_start);

    while (wiic_poll(wiimotes, MAX_WIIMOTES)) {
      for (i = 0; i < MAX_WIIMOTES; ++i) {
        switch (wiimotes[i]->event) {
          case WIIC_EVENT:
            button_callback(wiimotes[i]->unid, wiimotes[i]->btns);
            break;

          default:
            break;
        }
      }
    }

    while (elapsed < 100000) {
      clock_gettime(CLOCK_REALTIME, &ts_end);
      
      if (ts_end.tv_nsec < ts_start.tv_nsec) {
        elapsed += (NS_PER_SEC * (ts_end.tv_sec - ts_start.tv_sec - 1)) +
          (NS_PER_SEC + ts_end.tv_nsec - ts_start.tv_nsec);
      }
      else {
        elapsed += (NS_PER_SEC * (ts_end.tv_sec - ts_start.tv_sec)) +
          (ts_end.tv_nsec - ts_start.tv_nsec);
      }

      clock_gettime(CLOCK_REALTIME, &ts_start);
      usleep(1);
    }
  }

  printf("poll end\n");

  wiic_cleanup(wiimotes, MAX_WIIMOTES);
}

void wrapper_disconnect() {
  done = 1;
}

