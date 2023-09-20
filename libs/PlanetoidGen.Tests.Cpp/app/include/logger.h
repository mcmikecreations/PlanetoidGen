#ifndef LOGGER_H
#define LOGGER_H

#include <cstdarg>
#include <memory>			// for unique_ptr
#include <string>

class Logger
{
	private:
		static std::unique_ptr<Logger> _instance;
		FILE *logfile;
	protected:
		Logger ();
	public:
		static Logger& Instance();
		void SetLog (FILE *l);
		void Log (const char *fmt, ...);
		void Log (std::string msg);
};

#endif
