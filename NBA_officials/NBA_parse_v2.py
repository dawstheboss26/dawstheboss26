#!user/bin/env python3

import re
import requests
import json
import os
import multiprocessing as mp


beginning = 21300001
end = 21301000
def tryurl(i, q):
        try:
                url = 'https://www.nba.com/game/00'+str(i)
                r = requests.get(url)
                r.text
                find = re.compile(r'"application/json">(.*?)</script></body></html>')

                here = re.findall(find, r.text)
# here is now a json string

                game_official_dict = json.loads(here[0])
                print ("JSON LOADED")

                gameID = game_official_dict["props"]["pageProps"]["game"]["gameId"]
                gameDate = game_official_dict["props"]["pageProps"]["game"]["gameEt"][0:10]
                gameLength = game_official_dict["props"]["pageProps"]["game"]["duration"]

                string1Append = gameID+','+gameDate+','+gameLength
                q.put(string1Append)

        except:
                print("failed"+str(i)+"\n")
def listener(q):
        with open("game_occurrence.csv", 'a') as f:
                while 1:
                        m = q.get()
                        if m == 'kill':
                                break
                        f.write(str(m)+'\n')
                        f.flush()


manager = mp.Manager()
q = manager.Queue()
pool = mp.Pool(mp.cpu_count() - 2)

watcher = pool.apply_async(listener, (q,))
jobs = []
for i in range(beginning, end):
        job = pool.apply_async(tryurl, (i, q))
        jobs.append(job)
for job in jobs:
        job.get()
q.put('kill')
pool.close()
pool.join()
