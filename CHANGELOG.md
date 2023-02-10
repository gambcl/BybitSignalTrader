## [0.4.4](https://github.com/gambcl/SignalTrader/compare/v0.4.3...v0.4.4) (2023-02-10)


### Bug Fixes

* attempt to fix issue with order fills being assigned to wrong order in db ([0b6afe6](https://github.com/gambcl/SignalTrader/commit/0b6afe6afd9c1372cabaadf684dc614611f2c924))

## [0.4.3](https://github.com/gambcl/SignalTrader/compare/v0.4.2...v0.4.3) (2023-02-10)


### Bug Fixes

* show positions via telegram ([d25ce71](https://github.com/gambcl/SignalTrader/commit/d25ce714c595a7ecc8fe403d3858e3bc6de31f72))

## [0.4.2](https://github.com/gambcl/SignalTrader/compare/v0.4.1...v0.4.2) (2023-02-10)


### Bug Fixes

* fetch positions via rest api ([01f1f7f](https://github.com/gambcl/SignalTrader/commit/01f1f7febc696bce69a22468203907a267064d15))

## [0.4.1](https://github.com/gambcl/SignalTrader/compare/v0.4.0...v0.4.1) (2023-02-07)


### Bug Fixes

* updated nuget packages ([8df76a5](https://github.com/gambcl/SignalTrader/commit/8df76a5f32ea4196739c9fc8724358b9d689338c))

## [0.4.0](https://github.com/gambcl/SignalTrader/compare/v0.3.2...v0.4.0) (2023-02-06)


### Features

* create orders and positions ([57a878b](https://github.com/gambcl/SignalTrader/commit/57a878b5d9868471d12a44742b67c5bffe32c8b6))

## [0.3.2](https://github.com/gambcl/SignalTrader/compare/v0.3.1...v0.3.2) (2023-01-30)


### Bug Fixes

* accept integer values for ohlc values in signal ([398336b](https://github.com/gambcl/SignalTrader/commit/398336b12e3b723e9b7739ffd12e4fa835295460))

## [0.3.1](https://github.com/gambcl/SignalTrader/compare/v0.3.0...v0.3.1) (2023-01-29)


### Bug Fixes

* resolve async tree visitor issue and fix accountid type validation ([2a96193](https://github.com/gambcl/SignalTrader/commit/2a96193aa81e3dfe7c65330f05bb23bc01be2d9f))

## [0.3.0](https://github.com/gambcl/SignalTrader/compare/v0.2.2...v0.3.0) (2023-01-29)


### Features

* support signals with signalscript body ([ef34c66](https://github.com/gambcl/SignalTrader/commit/ef34c66ae685dbac59acaeded3bdcf27f1074414))

## [0.2.2](https://github.com/gambcl/SignalTrader/compare/v0.2.1...v0.2.2) (2023-01-22)


### Bug Fixes

* signals controller should accept plain text body ([8974840](https://github.com/gambcl/SignalTrader/commit/8974840c6a44c5e060e8dd0d8d5b3de55f7beca4))

## [0.2.1](https://github.com/gambcl/SignalTrader/compare/v0.2.0...v0.2.1) (2023-01-21)


### Bug Fixes

* change default postgres host ([78daecb](https://github.com/gambcl/SignalTrader/commit/78daecbb615d82c1ba86011ff71fb73ec45fb528))
* expose port 8000 in dockerfile ([3d73a5c](https://github.com/gambcl/SignalTrader/commit/3d73a5ca9c22fbc0925c5b07659753d766c9469c))

## [0.2.0](https://github.com/gambcl/SignalTrader/compare/v0.1.0...v0.2.0) (2023-01-21)


### Features

* add support for docker secrets ([0e7eff7](https://github.com/gambcl/SignalTrader/commit/0e7eff7385900a7191936fc25b668ba25662c072))
* added accounts api and postgres database ([c18020c](https://github.com/gambcl/SignalTrader/commit/c18020cbaf52a711bc21959f95d937f5c62a2dc6))
* show account balances via api and telegram ([53719d5](https://github.com/gambcl/SignalTrader/commit/53719d58c1b28240f3d204e85f706f29b7659899))
* user authentication using jwt ([9adf269](https://github.com/gambcl/SignalTrader/commit/9adf2698e3dd8ba90c31d983b57c71b5bd6b353d))

## [0.1.0](https://github.com/gambcl/SignalTrader/compare/v0.0.0...v0.1.0) (2023-01-16)


### Features

* create webapi project ([d8d054c](https://github.com/gambcl/SignalTrader/commit/d8d054c8ab2f4fcf2f7cdccbd42d095ea1811a79))
* configure logging
* handle tradingview webhook requests
* add telegram support
