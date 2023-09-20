#ifndef AGENT_H
#define AGENT_H

#include <string>

typedef enum {SHORELINE_AGENT, MOUNTAIN_AGENT, SMOOTH_AGENT, RIVER_AGENT, EROSION_AGENT, HILL_AGENT} AgentType;

class Agent
{
public:
	virtual bool Execute () = 0;
	inline AgentType getType()			{return type;}
	inline bool isRunnable()			{return runnable;}
	inline void setRunnable (bool b)	{runnable = b;}
	inline std::string getName()		{return name;}

protected:
	AgentType type;
	unsigned int tokens;
	bool runnable;
	int id;
	std::string name;
};

#endif