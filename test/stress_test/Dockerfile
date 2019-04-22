FROM python:3.6
RUN pip install locustio pyzmq websocket-client
RUN mkdir /app
WORKDIR /app
ADD . /app/
EXPOSE 8089
