import os
import pymongo

def get_document(document_id):
    client = pymongo.MongoClient("mongodb://localhost:30038/")

    db = client["PlanetoidGenDocs"]
    collection = db["FileContent"]

    return collection.find_one({"_id": document_id})

def save_document(document):
    content = document['Content']
    path = "debug/" + document['LocalPath']
    filepath = path + "/" + document['FileName']

    if not os.path.exists(path):
        os.makedirs(path)
        print("Path created:", path)

    with open(filepath, "wb") as f:
        f.write(content)

    return filepath


#data_type = 'com.PlanetoidGen.Procedural.HeightMapGrayscaleEncoded'
#data_type_rgba32 = 'com.PlanetoidGen.Procedural.HeightMapRgba32Encoded'
#planetoid_id = 8
#z = 0
#x_arr = [0]
#y_arr = [0, 1, 2, 3, 4, 5]

#for x in x_arr:
#    for y in y_arr:
#        document = get_document(f"Planetoid_{planetoid_id}/{data_type}/{z}/{x}/{y}")
#        filepath = save_document(document)
#        print(filepath)

ids = [
    "Planetoid_13/com.PlanetoidGen.Procedural.HeightMapGrayscaleEncoded/1/0/0",
    "Planetoid_13/com.PlanetoidGen.Procedural.HeightMapGrayscaleEncoded/1/0/6",
    "Planetoid_13/com.PlanetoidGen.Procedural.HeightMapGrayscaleEncoded/1/1/0",
    "Planetoid_13/com.PlanetoidGen.Procedural.HeightMapGrayscaleEncoded/1/1/6",
    "Planetoid_13/com.PlanetoidGen.Procedural.HeightMapGrayscaleEncoded/1/0/1",
    "Planetoid_13/com.PlanetoidGen.Procedural.HeightMapGrayscaleEncoded/1/0/7",
    "Planetoid_13/com.PlanetoidGen.Procedural.HeightMapGrayscaleEncoded/1/1/1",
    "Planetoid_13/com.PlanetoidGen.Procedural.HeightMapGrayscaleEncoded/1/1/7",
    "Planetoid_13/com.PlanetoidGen.Procedural.HeightMapGrayscaleEncoded/1/0/2",
    "Planetoid_13/com.PlanetoidGen.Procedural.HeightMapGrayscaleEncoded/1/0/8",
    "Planetoid_13/com.PlanetoidGen.Procedural.HeightMapGrayscaleEncoded/1/1/2",
    "Planetoid_13/com.PlanetoidGen.Procedural.HeightMapGrayscaleEncoded/1/1/8",
    "Planetoid_13/com.PlanetoidGen.Procedural.HeightMapGrayscaleEncoded/1/0/3",
    "Planetoid_13/com.PlanetoidGen.Procedural.HeightMapGrayscaleEncoded/1/0/9",
    "Planetoid_13/com.PlanetoidGen.Procedural.HeightMapGrayscaleEncoded/1/1/3",
    "Planetoid_13/com.PlanetoidGen.Procedural.HeightMapGrayscaleEncoded/1/1/9",
    "Planetoid_13/com.PlanetoidGen.Procedural.HeightMapGrayscaleEncoded/1/0/4",
    "Planetoid_13/com.PlanetoidGen.Procedural.HeightMapGrayscaleEncoded/1/0/10",
    "Planetoid_13/com.PlanetoidGen.Procedural.HeightMapGrayscaleEncoded/1/1/4",
    "Planetoid_13/com.PlanetoidGen.Procedural.HeightMapGrayscaleEncoded/1/1/10",
    "Planetoid_13/com.PlanetoidGen.Procedural.HeightMapGrayscaleEncoded/1/0/5",
    "Planetoid_13/com.PlanetoidGen.Procedural.HeightMapGrayscaleEncoded/1/0/11",
    "Planetoid_13/com.PlanetoidGen.Procedural.HeightMapGrayscaleEncoded/1/1/5",
    "Planetoid_13/com.PlanetoidGen.Procedural.HeightMapGrayscaleEncoded/1/1/11"
]

for id in ids:
    document = get_document(id)
    filepath = save_document(document)
    print(filepath)