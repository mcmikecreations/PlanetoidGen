#include "action.h"

Point::Point ()
{
	x = 0;
	y = 0;
	set = false;
}

Point::Point(int x1, int y1)
{
	x = x1;
	y = y1;
	set = true;
}

bool Point::operator== (const Point& right) const
{
	if ((x == right.x) && (y == right.y))
		return true;

	return false;
}

bool Point::operator!= (const Point& right) const
{
	return (! (*this == right));
}

bool Point::operator< (const Point& right) const
{
	if ((x < right.x) || ((x == right.x) && (y < right.y)))
		return true;
	else
		return false;
}
