# -*- coding:utf-8 -*-
from __future__ import absolute_import
from __future__ import unicode_literals
from __future__ import print_function

import json
import uuid
import time
import gevent
import os

from websocket import create_connection
import redis

from locust import HttpLocust, TaskSet, task
from locust.events import request_success

ENDPOINT = os.environ.get('WS_URL', 'ws://host.docker.internal:5110/ws/')
REDIS = os.environ.get('REDIS', 'host.docker.internal')
CONNECTION_ID_KEY = 'locust:connection_id'

class WebSocketTaskSet(TaskSet):
    def on_start(self):
        self.rd = redis.Redis(host=REDIS, port=6379, db=0)
        self.connection_id = self.rd.incr(CONNECTION_ID_KEY)
        self.ws = create_connection(ENDPOINT + str(self.connection_id))

        def _receive():
            while True:
                res = self.ws.recv()
                data = json.loads(res.decode())
                end_at = time.time()
                response_time = int(round(end_at - data['start_at'], 3) * 1000)
                request_success.fire(
                    request_type='WebSocketTest',
                    name='recv',
                    response_time=response_time,
                    response_length=len(res),
                )

        gevent.spawn(_receive)

    @task
    def sent(self):
        start_at = time.time()
        body = json.dumps({'message': 'hello, world', 'connection_id': self.connection_id, 'start_at': start_at})
        self.ws.send_binary(body.encode())
        request_success.fire(
            request_type='WebSocketTest',
            name='send',
            response_time = int(round(time.time() - start_at, 3) * 1000),
            response_length=len(body),
        )

    def teardown(self):
        self.ws.close()
        self.rd.delete(CONNECTION_ID_KEY)


class WebSocketLocust(HttpLocust):
    task_set = WebSocketTaskSet
    min_wait = 1000
    max_wait = 5000