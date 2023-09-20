#ifndef INDEX_H
#define INDEX_H

#include "image.h"

class Index : public Image
{
public:
	Index(uint x, uint y);
	virtual ~Index ();

	void SetPrimary (uint x, uint y, uchar texture_id, uchar alpha);
	void SetSecondary (uint x, uint y, uchar texture_id, uchar alpha);
	uchar PrimaryAlpha (uint x, uint y);
	uchar PrimaryTexture (uint x, uint y);
	uchar SecondaryAlpha (uint x, uint y);
	uchar SecondaryTexture (uint x, uint y);
};

#endif
