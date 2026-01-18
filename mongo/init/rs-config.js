try {
  rs.status();
} catch (e) {
  rs.initiate({
    _id: "rs-config",
    configsvr: true,
    members: [{ _id: 0, host: "configsvr:27019" }]
  });
}
