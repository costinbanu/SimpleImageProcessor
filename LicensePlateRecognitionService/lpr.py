from typing import List
import cv2
import json
from fastanpr import FastANPR
from fastanpr import NumberPlate

class Result:
    def __init__(self, file: str, number_plate: List[NumberPlate]):
        self.file = file
        self.number_plate = number_plate

async def run(files: List[str]) -> str:
    fast_anpr = FastANPR()

    images = [cv2.cvtColor(cv2.imread(file), cv2.COLOR_BGR2RGB) for file in files]

    number_plates = await fast_anpr.run(images)

    to_return = []
    for file, plates in zip(files, number_plates):
        to_return.append(Result(file, plates))
    
    return json.dumps(to_return, default=lambda o: o.__dict__, indent=4);