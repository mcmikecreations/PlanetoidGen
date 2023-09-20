#include "logger.h"

Logger::Logger ()
{
	logfile = NULL;
}

std::unique_ptr<Logger> Logger::_instance;
Logger& Logger::Instance()
{
	if (_instance.get() == NULL)
	{
		_instance.reset (new Logger);
	}

	return *_instance;
}

// ===================================================================
// Log -- log a message to a file, like printf
// ===================================================================
void Logger::Log (const char *fmt, ...)
{
	va_list ap;

	if (logfile != NULL)
	{
		va_start (ap, fmt);
		vfprintf (logfile, fmt, ap);
		va_end (ap);
	}
}

void Logger::SetLog (FILE *l)
{
	logfile = l;
	setbuf (logfile, NULL);
}


// ===================================================================
// Log -- log a message to a file, using std::string
// ===================================================================
void Logger::Log (std::string msg)
{
	if (logfile != NULL)
	{
		fprintf (logfile, "%s\n", msg.c_str());
	}
}
