FROM ubuntu:latest

RUN apt-get update && apt-get install -y \
    wget \
    unzip \
    git \
    vim \
    libicu[0-9][0-9]

RUN apt-get update && apt-get install -y \
    python3.6 \
    python3.6-dev \
    python3-pip

RUN pip install --upgrade pip \
 && pip install pylint pyzmq websocket-client redis \
 && pip install -e git://github.com/locustio/locust.git@master#egg=locustio
