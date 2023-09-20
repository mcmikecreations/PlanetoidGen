#include "arglist.h"
#include <stdlib.h>
#include <string.h>
#include <stdio.h>
#include <string>
#include <fstream>
#include <iostream>

using namespace std;

Arglist::Arglist ()
{

}

void Arglist::Set (int c, char **v)
{
	for (int i = 0; i < c; i++)
	{
		string arg(v[i]);
		args.push_back (arg);
	}
}

void Arglist::Set (string cmdline)
{
	int pos;
	string line = cmdline;

	while ((pos = line.find(' ')) != std::string::npos)
	{
		string arg = line.substr(0, pos);
		args.push_back(arg);

		pos = line.find_first_not_of(" ", pos);
		line.erase (0, pos);
	}

	if (line.length() > 0)
	{
		args.push_back (line);
	}
}

// =======================================================
// Save an argument list to a file
// =======================================================
int Arglist::Save (const char *filename)
{
	ofstream file(filename);

	if (! file)
	{
		return -1;
	}

	ALStringVector::iterator iter;

	for (iter = args.begin(); iter != args.end(); ++iter)
	{
		if (iter != args.begin())
		{
			file << " ";
		}

		file << *iter;
	}

	file << endl;
	file.close();
	return args.size();
}

// =======================================================
// Load a command line
// =======================================================

int Arglist::Load (char *filename)
{
	ifstream file(filename);
	int pos;
	int argpos;

	if (! file)
	{
		return -1;
	}

	string line;
	while (! file.eof())
	{
		file >> line;
		if (line.compare("mapgen") == 0)
			continue;

		args.push_back(line);

#if 0
		pos = line.find("-");
		argpos = line.find (" ");
		
		if (pos != string::npos)
		{
			if (argpos != string::npos)
			{
				args.push_back(line.substr(pos, argpos-pos));
				args.push_back(line.substr(argpos+1));
			}
		}
#endif
	}

	file.close();
	return args.size();
}

void Arglist::Display ()
{
	ALStringVector::iterator iter;
	int count = 0;

	for (iter = args.begin(); iter != args.end(); ++iter)
	{
		cout << "argv[" << count++ << "] = " << *iter << endl;
	}
}

string
Arglist::getArg(unsigned int i)
{
	if (i > args.size())
	{
		return "";
	}

	return args[i];
}
