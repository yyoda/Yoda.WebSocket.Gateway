# -*- coding:utf-8 -*-
from __future__ import absolute_import
from __future__ import unicode_literals
from __future__ import print_function

import json
import uuid
import time
import gevent

from websocket import create_connection
import six

from locust import HttpLocust, TaskSet, task
from locust.events import request_success

ENDPOINT = 'ws://localhost:5000/ws/'

class WebSocketTaskSet(TaskSet):
    def on_start(self):
        self.user_id = six.text_type(uuid.uuid4())
        ws = create_connection(ENDPOINT + self.user_id)
        self.ws = ws

        def _receive():
            while True:
                res = ws.recv()
                data = json.loads(res.decode())
                end_at = time.time()
                response_time = int((end_at - data['start_at']) * 1000000)
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
        body = json.dumps({'message': 'hello, world', 'user_id': self.user_id, 'start_at': start_at})
        self.ws.send_binary(body.encode())
        request_success.fire(
            request_type='WebSocketTest',
            name='send',
            response_time=int((time.time() - start_at) * 1000000),
            response_length=len(body),
        )


class WebSocketLocust(HttpLocust):
    task_set = WebSocketTaskSet
    min_wait = 50
    max_wait = 100