<?php
/**
 * Created by PhpStorm.
 * User: famoser
 * Date: 04.11.2016
 * Time: 17:10
 */

namespace Famoser\SyncApi\Models\Entities;

/*
CREATE TABLE 'devices' (
  'id'               INTEGER DEFAULT NULL PRIMARY KEY AUTOINCREMENT,
  'user_guid'        TEXT    DEFAULT NULL,
  'identifier'       TEXT    DEFAULT NULL,
  'guid'             TEXT    DEFAULT NULL
);
*/

use Famoser\SyncApi\Models\Entities\Base\BaseEntity;

class Device extends BaseEntity
{
    public $user_guid;
    public $identifier;
    public $guid;

    public function getTableName()
    {
        return "devices";
    }
}