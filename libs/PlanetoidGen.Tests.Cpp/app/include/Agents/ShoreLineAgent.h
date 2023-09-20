#ifndef SHORELINE_AGENT_H
#define SHORELINE_AGENT_H

#include "agent.h"
#include "point.h"

class ShoreLineAgent : public Agent
{
public:
	ShoreLineAgent (int _tokens);
	bool Execute ();

private:
	Point location;
	static int count;

	bool adjacentShore (Point& p);
	void miniWalk (Point& location);
};

#endif
