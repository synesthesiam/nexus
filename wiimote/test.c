#include <stdio.h>
#include <wiic.h>

#define MAX_WIIMOTES 7

int main() {
  wiimote** wiimotes;
  int numConnected = 0;

  wiimotes = wiic_init(MAX_WIIMOTES);

  int numFound = wiic_find(wiimotes, MAX_WIIMOTES, 5);
  int i, result;
  char c;

  c = 'a';

  while ((c != 'q') && (numConnected < numFound)) {
    for (i = 0; i < numFound; ++i) {
      if (!WIIMOTE_IS_CONNECTED(wiimotes[i])) {
        printf("Press any key to connect %s\n", wiimotes[i]->bdaddr_str);
        c = getchar();
        if (wiic_connect_one(wiimotes, i)) {
          ++numConnected;
          wiic_set_leds(wiimotes[i], WIIMOTE_LED_1);
        }
      }
    }
  }

  /*if (numFound > 0) {*/
    /*numConnected = wiic_connect(wiimotes, numFound);*/

    /*for (i = 0; i < MAX_WIIMOTES; ++i) {*/
      /*if (WIIMOTE_IS_CONNECTED(wiimotes[i])) {*/
        /*wiic_set_leds(wiimotes[i], WIIMOTE_LED_1); */
        /*[>wiic_rumble(wiimotes[i], 1);<]*/
        /*[>usleep(500000);<]*/
        /*[>wiic_rumble(wiimotes[i], 0);<]*/
        /*wiic_status(wiimotes[i]);*/
      /*}*/
    /*}*/
  /*}*/

  /*while (1) {*/
    /*if (wiic_poll(wiimotes, MAX_WIIMOTES)) {*/
      /*for (i = 0; i < MAX_WIIMOTES; ++i) {*/
        /*if (WIIMOTE_IS_CONNECTED(wiimotes[i])) {*/
          /*switch (wiimotes[i]->event) {*/
            /*case WIIC_EVENT:*/
            /*case WIIC_STATUS:*/
            /*case WIIC_DISCONNECT:*/
              /*printf("%d\n", i);*/
              /*[>handle_event(wiimotes[i]);<]*/
              /*break;*/

            /*default:*/
              /*[>printf("%d?\n", wiimotes[i]->event);<]*/
              /*break;*/
          /*}*/
        /*}*/
      /*}*/
    /*}*/
  /*}*/

  return (0);
}

