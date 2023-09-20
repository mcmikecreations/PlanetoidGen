#ifndef HILLAGENT_H
#define HILLAGENT_H

#include "agent.h"
#include "point.h"
#include "heightmap.h"

class HillAgent : public Agent
{
public:
	HillAgent (int _tokens);
	bool Execute ();

private:
	static int count;
	int initialized;
	Point location;
};

#endif
