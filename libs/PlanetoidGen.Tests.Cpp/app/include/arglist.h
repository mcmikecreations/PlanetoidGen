#ifndef ARGLIST_H
#define ARGLIST_H

#include <string>
#include <vector>

typedef std::vector<std::string> ALStringVector;

class Arglist
{
public:
	Arglist ();

	void Set (std::string cmdline);
	void Set (int argc, char **argv);
	void Display ();
	int Save (const char *filename);
	int Load (char *filename);
	inline unsigned int count()	{return args.size(); }
	std::string getArg(unsigned int i);
private:
	ALStringVector args;
};

#endif